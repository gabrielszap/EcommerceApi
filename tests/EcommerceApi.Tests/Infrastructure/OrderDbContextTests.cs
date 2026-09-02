using EcommerceApi.Infrastructure;
using EcommerceApi.Infrastructure.Persistence;
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
                $"INSERT INTO OrderItems (Id, OrderId, ProductName, Quantity, UnitPrice) VALUES ({quantityItemId}, {orderId}, {productName}, 0, 10.00);") );

        var priceItemId = Guid.NewGuid();
        await Assert.ThrowsAsync<SqliteException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO OrderItems (Id, OrderId, ProductName, Quantity, UnitPrice) VALUES ({priceItemId}, {orderId}, {productName}, 1, -0.01);") );

        var zeroPriceItemId = Guid.NewGuid();
        await Assert.ThrowsAsync<SqliteException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO OrderItems (Id, OrderId, ProductName, Quantity, UnitPrice) VALUES ({zeroPriceItemId}, {orderId}, {productName}, 1, 0.0);") );
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
