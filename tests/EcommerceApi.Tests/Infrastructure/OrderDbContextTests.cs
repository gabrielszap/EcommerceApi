using EcommerceApi.Infrastructure;
using EcommerceApi.Infrastructure.Persistence;
using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Orders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Tests.Infrastructure;

public sealed class OrderDbContextTests
{
    [Fact]
    public async Task AddInfrastructure_CreatesParentDirectoryForNewSqliteFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ecommerceapi-missing-parent-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "nested", "orders.db");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Orders"] = $"Data Source={path}"
                })
                .Build();
            var services = new ServiceCollection();

            services.AddInfrastructure(configuration);
            await using var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();

            Assert.True(File.Exists(path));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MigrateAsync_OnFreshDatabase_CreatesOrderSchemaWithoutTotalAmount()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using var context = database.CreateContext();

        await context.Database.MigrateAsync();

        var tables = await database.QueryStringsAsync(
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;");
        var orderColumns = await database.QueryStringsAsync("PRAGMA table_info('Orders');", 1);

        Assert.Contains("Orders", tables);
        Assert.Contains("OrderItems", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
        Assert.DoesNotContain("TotalAmount", orderColumns);
    }

    [Fact]
    public async Task MigrateAsync_WhenMigrationAlreadyApplied_IsIdempotent()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using (var firstContext = database.CreateContext())
        {
            await firstContext.Database.MigrateAsync();
        }

        await using (var secondContext = database.CreateContext())
        {
            await secondContext.Database.MigrateAsync();
        }

        var migrations = await database.QueryStringsAsync(
            "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;");

        Assert.Single(migrations);
        Assert.EndsWith("_InitialOrderSchema", migrations.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Migration_ConfiguresRequiredCascadeOrderRelationship()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        await using var connection = new SqliteConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_list('OrderItems');";
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("Orders", reader.GetString(reader.GetOrdinal("table")));
        Assert.Equal("OrderId", reader.GetString(reader.GetOrdinal("from")));
        Assert.Equal("CASCADE", reader.GetString(reader.GetOrdinal("on_delete")));
    }

    [Fact]
    public async Task Migration_RejectsInvalidStatusQuantityAndPrice()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        const string createdAt = "2026-09-02T12:00:00Z";
        const string productName = "Keyboard";
        await Assert.ThrowsAsync<SqliteException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Orders (Id, CustomerId, Status, CreatedAt) VALUES ({orderId}, {customerId}, 99, {createdAt});"));

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Orders (Id, CustomerId, Status, CreatedAt) VALUES ({orderId}, {customerId}, 0, {createdAt});");

        var quantityItemId = Guid.NewGuid();
        await Assert.ThrowsAsync<SqliteException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO OrderItems (Id, OrderId, ProductName, Quantity, UnitPrice) VALUES ({quantityItemId}, {orderId}, {productName}, 0, 10.00);"));

        var priceItemId = Guid.NewGuid();
        await Assert.ThrowsAsync<SqliteException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO OrderItems (Id, OrderId, ProductName, Quantity, UnitPrice) VALUES ({priceItemId}, {orderId}, {productName}, 1, -0.01);"));

        var zeroPriceItemId = Guid.NewGuid();
        await Assert.ThrowsAsync<SqliteException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO OrderItems (Id, OrderId, ProductName, Quantity, UnitPrice) VALUES ({zeroPriceItemId}, {orderId}, {productName}, 1, 0.0);"));
    }

    [Fact]
    public async Task EfCoreOrderWriter_PersistsOrderAndItemsRelationshipAtomically()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();
        }

        var customerId = Guid.NewGuid();
        var order = Order.Create(
            customerId,
            [OrderItem.Create("Keyboard", 2, 150.00m), OrderItem.Create("Mouse", 1, 75.50m)],
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));

        await using (var writeContext = database.CreateContext())
        {
            var writer = new EfCoreOrderWriter(writeContext);
            await writer.AddAsync(order, CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var persisted = await readContext.Orders
            .Include("_items")
            .SingleAsync(savedOrder => savedOrder.Id == order.Id);

        Assert.Equal(customerId, persisted.CustomerId);
        Assert.Equal(OrderStatus.Pending, persisted.Status);
        Assert.Equal(2, persisted.Items.Count);
        Assert.All(persisted.Items, item => Assert.Equal(order.Id, item.OrderId));
        Assert.Equal(375.50m, persisted.TotalAmount);
    }

    [Fact]
    public async Task EfCoreOrderWriter_CancelledStatusPersistsAndSurvivesNewContext()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();
        }

        var order = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Keyboard", 2, 150.00m)],
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));

        await using (var seedContext = database.CreateContext())
        {
            var writer = new EfCoreOrderWriter(seedContext);
            await writer.AddAsync(order, CancellationToken.None);
        }

        await using (var cancelContext = database.CreateContext())
        {
            var writer = new EfCoreOrderWriter(cancelContext);
            var persisted = await writer.GetByIdForUpdateAsync(order.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            persisted.Cancel();
            await writer.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var cancelled = await readContext.Orders
            .Include("_items")
            .SingleAsync(savedOrder => savedOrder.Id == order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(300.00m, cancelled.TotalAmount);
    }

    [Fact]
    public async Task EfCoreOrderWriter_WhenStatusChangedByAnotherContext_ThrowsConcurrencyException()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();
        }

        var order = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Keyboard", 1, 100.00m)],
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));

        await using (var seedContext = database.CreateContext())
        {
            var writer = new EfCoreOrderWriter(seedContext);
            await writer.AddAsync(order, CancellationToken.None);
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstWriter = new EfCoreOrderWriter(firstContext);
        var secondWriter = new EfCoreOrderWriter(secondContext);
        var firstOrder = await firstWriter.GetByIdForUpdateAsync(order.Id, CancellationToken.None);
        var secondOrder = await secondWriter.GetByIdForUpdateAsync(order.Id, CancellationToken.None);

        Assert.NotNull(firstOrder);
        Assert.NotNull(secondOrder);
        firstOrder.Cancel();
        secondOrder.Cancel();
        await firstWriter.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<OrderPersistenceConcurrencyException>(() =>
            secondWriter.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EfCoreOrderReader_ReturnsDeterministicPagesAndDetailsWithDomainCalculatedTotals()
    {
        await using var database = TemporarySqliteDatabase.Create();
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();
        }

        var oldest = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Notebook", 1, 100.00m)],
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));
        var newestA = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Keyboard", 2, 150.00m)],
            new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc));
        var newestB = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Mouse", 3, 50.00m), OrderItem.Create("Pad", 1, 25.00m)],
            new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc));

        await using (var writeContext = database.CreateContext())
        {
            var writer = new EfCoreOrderWriter(writeContext);
            await writer.AddAsync(oldest, CancellationToken.None);
            await writer.AddAsync(newestA, CancellationToken.None);
            await writer.AddAsync(newestB, CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var reader = new EfCoreOrderReader(readContext);

        var firstPage = await reader.GetPageAsync(1, 2, CancellationToken.None);
        var secondPage = await reader.GetPageAsync(2, 2, CancellationToken.None);
        var expectedOrderIds = new[] { newestA, newestB, oldest }
            .OrderByDescending(order => order.CreatedAt)
            .ThenBy(order => order.Id)
            .Select(order => order.Id)
            .ToArray();
        var detail = await reader.GetByIdAsync(newestB.Id, CancellationToken.None);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(expectedOrderIds.Take(2), firstPage.Items.Select(order => order.Id));
        Assert.Equal(expectedOrderIds.Skip(2), secondPage.Items.Select(order => order.Id));
        Assert.Equal(3, secondPage.TotalCount);
        Assert.NotNull(detail);
        Assert.Equal(newestB.Id, detail.Id);
        Assert.Equal(2, detail.Items.Count);
        Assert.Equal(175.00m, detail.TotalAmount);
    }

    private sealed class TemporarySqliteDatabase : IAsyncDisposable
    {
        private TemporarySqliteDatabase(string path)
        {
            Path = path;
            ConnectionString = $"Data Source={path}";
        }

        private string Path { get; }

        public string ConnectionString { get; }

        public static TemporarySqliteDatabase Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ecommerceapi-tests-{Guid.NewGuid():N}.db");
            return new TemporarySqliteDatabase(path);
        }

        public OrderDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseSqlite(ConnectionString)
                .Options;
            return new OrderDbContext(options);
        }

        public async Task<IReadOnlyList<string>> QueryStringsAsync(string sql, int ordinal = 0)
        {
            var values = new List<string>();
            await using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(ordinal));
            }

            return values;
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }

            return ValueTask.CompletedTask;
        }
    }
}
