using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceApi.Domain.Orders;
using EcommerceApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Tests.Api;

[Collection("QueryOrdersApi")]
public sealed class QueryOrdersEndpointTests
{
    [Fact]
    public async Task GetOrders_WithValidBearerToken_ReturnsPagedNewestFirstOrders()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        var firstOrder = await SeedOrderAsync(
            factory,
            "Notebook",
            1,
            100.00m,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));
        var secondOrder = await SeedOrderAsync(
            factory,
            "Keyboard",
            2,
            150.00m,
            new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc));

        var response = await client.GetAsync("/api/orders?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
        Assert.False(root.GetProperty("hasPreviousPage").GetBoolean());
        Assert.True(root.GetProperty("hasNextPage").GetBoolean());
        var items = root.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(secondOrder, items[0].GetProperty("id").GetGuid());
        Assert.NotEqual(firstOrder, items[0].GetProperty("id").GetGuid());
        Assert.Equal(1, items[0].GetProperty("itemCount").GetInt32());
        Assert.Equal(300.00m, items[0].GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public async Task GetOrders_WithInvalidPagination_ReturnsBadRequest()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync("/api/orders?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetOrders_WithMalformedPagination_ReturnsBadRequestProblem()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync("/api/orders?page=abc&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetOrders_WithPageSizeAboveLimit_ReturnsBadRequest()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync("/api/orders?page=1&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetOrderById_WithExistingOrder_ReturnsOrderAndItems()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        var orderId = await CreateOrderAsync(client, "Keyboard", 2, 150.00m);

        var response = await client.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        Assert.Equal(orderId, root.GetProperty("id").GetGuid());
        Assert.Equal("Pending", root.GetProperty("status").GetString());
        Assert.Equal(300.00m, root.GetProperty("totalAmount").GetDecimal());
        Assert.Single(root.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task GetOrderById_WithMissingOrder_ReturnsNotFoundProblem()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetOrderById_WithMalformedGuid_ReturnsBadRequestProblem()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync("/api/orders/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetOrderById_WithEmptyGuid_ReturnsBadRequestProblem()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync($"/api/orders/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/orders")]
    [InlineData("/api/orders/11111111-1111-1111-1111-111111111111")]
    public async Task GetOrderRoutes_WithoutBearerToken_ReturnUnauthorized(string route)
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesBearerSecurityForQueryEndpoints()
    {
        using var factory = new QueryOrdersApiFactory();
        using var client = factory.CreateClient();

        using var document = await JsonDocument.ParseAsync(
            await client.GetStreamAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var listOperation = paths.GetProperty("/api/orders").GetProperty("get");
        var detailOperation = paths.GetProperty("/api/orders/{id}").GetProperty("get");

        AssertOperationRequiresBearer(listOperation);
        AssertOperationRequiresBearer(detailOperation);
        AssertParameterSchema(listOperation, "page", "integer", minimum: 1);
        AssertParameterSchema(listOperation, "pageSize", "integer", minimum: 1, maximum: 100);
        AssertParameterSchema(detailOperation, "id", "string", format: "uuid");
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

    private static async Task<Guid> CreateOrderAsync(
        HttpClient client,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId = Guid.NewGuid(),
                items = new[] { new { productName, quantity, unitPrice } }
            });
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> SeedOrderAsync(
        QueryOrdersApiFactory factory,
        string productName,
        int quantity,
        decimal unitPrice,
        DateTime createdAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create(productName, quantity, unitPrice)],
            createdAt);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order.Id;
    }

    private static void AssertOperationRequiresBearer(JsonElement operation)
    {
        var security = operation.GetProperty("security");
        Assert.Contains(security.EnumerateArray(), requirement => requirement.TryGetProperty("Bearer", out _));
    }

    private static void AssertParameterSchema(
        JsonElement operation,
        string name,
        string type,
        string? format = null,
        int? minimum = null,
        int? maximum = null)
    {
        var parameter = operation.GetProperty("parameters")
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == name);
        var schema = parameter.GetProperty("schema");

        Assert.Equal(type, schema.GetProperty("type").GetString());
        if (format is not null)
        {
            Assert.Equal(format, schema.GetProperty("format").GetString());
        }

        if (minimum is not null)
        {
            Assert.Equal(minimum.Value, schema.GetProperty("minimum").GetInt32());
        }

        if (maximum is not null)
        {
            Assert.Equal(maximum.Value, schema.GetProperty("maximum").GetInt32());
        }
    }

    private sealed class QueryOrdersApiFactory : WebApplicationFactory<Program>
    {
        private const string SigningKey = "test-signing-key-with-at-least-32-bytes";

        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"ecommerceapi-query-orders-{Guid.NewGuid():N}.db");

        private readonly Dictionary<string, string?> _previousEnvironmentValues = [];

        public QueryOrdersApiFactory()
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

[CollectionDefinition("QueryOrdersApi", DisableParallelization = true)]
public sealed class QueryOrdersApiCollection
{
}
