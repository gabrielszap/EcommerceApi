using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Tests.Api;

[Collection("CreateOrderApi")]
public sealed class CreateOrderEndpointTests
{
    [Fact]
    public async Task PostOrders_WithValidBearerToken_ReturnsCreatedAndPersistsOrder()
    {
        using var factory = new CreateOrderApiFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var customerId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId,
                items = new[]
                {
                    new { productName = "Keyboard", quantity = 2, unitPrice = 150.00m },
                    new { productName = "Mouse", quantity = 1, unitPrice = 75.50m }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var orderId = root.GetProperty("id").GetGuid();
        Assert.Equal($"/api/orders/{orderId}", response.Headers.Location!.OriginalString);
        Assert.Equal(customerId, root.GetProperty("customerId").GetGuid());
        Assert.Equal("Pending", root.GetProperty("status").GetString());
        Assert.Equal(375.50m, root.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var persisted = await context.Orders
            .Include("_items")
            .SingleAsync(order => order.Id == orderId);
        Assert.Equal(customerId, persisted.CustomerId);
        Assert.Equal(375.50m, persisted.TotalAmount);
        Assert.Equal(2, persisted.Items.Count);
    }

    [Fact]
    public async Task PostOrders_WithoutBearerToken_ReturnsUnauthorized()
    {
        using var factory = new CreateOrderApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId = Guid.NewGuid(),
                items = new[] { new { productName = "Keyboard", quantity = 1, unitPrice = 10.00m } }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostOrders_WithInvalidItem_ReturnsValidationProblemAndPersistsNothing()
    {
        using var factory = new CreateOrderApiFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId = Guid.NewGuid(),
                items = new[] { new { productName = "Keyboard", quantity = 0, unitPrice = 10.00m } }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        Assert.Equal(0, await context.Orders.CountAsync());
        Assert.Equal(0, await context.OrderItems.CountAsync());
    }

    [Fact]
    public async Task PostOrders_WithNullItem_ReturnsValidationProblemAndPersistsNothing()
    {
        using var factory = new CreateOrderApiFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId = Guid.NewGuid(),
                items = new object?[] { null }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        Assert.Equal(0, await context.Orders.CountAsync());
        Assert.Equal(0, await context.OrderItems.CountAsync());
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new { email = "dev@martech.com", password = "Senha@123" });
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    private sealed class CreateOrderApiFactory : WebApplicationFactory<Program>
    {
        private const string SigningKey = "test-signing-key-with-at-least-32-bytes";

        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"ecommerceapi-create-order-{Guid.NewGuid():N}.db");

        private readonly Dictionary<string, string?> _previousEnvironmentValues = [];

        public CreateOrderApiFactory()
        {
            SetEnvironmentVariable("ConnectionStrings__Orders", $"Data Source={_databasePath}");
            SetEnvironmentVariable("Jwt__Issuer", "EcommerceApi");
            SetEnvironmentVariable("Jwt__Audience", "EcommerceApi.Client");
            SetEnvironmentVariable("Jwt__LifetimeMinutes", "60");
            SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var (key, value) in _previousEnvironmentValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }

            SqliteConnection.ClearAllPools();
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }

        private void SetEnvironmentVariable(string key, string value)
        {
            _previousEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

[CollectionDefinition("CreateOrderApi", DisableParallelization = true)]
public sealed class CreateOrderApiCollection
{
}
