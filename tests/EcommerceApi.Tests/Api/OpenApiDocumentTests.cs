using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace EcommerceApi.Tests.Api;

[Collection("OpenApi")]
public sealed class OpenApiDocumentTests
{
    [Fact]
    public async Task OpenApiDocument_InDevelopment_DescribesMetadataSecurityExamplesAndResponses()
    {
        using var factory = new OpenApiFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        Assert.Equal("EcommerceApi", root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", root.GetProperty("info").GetProperty("version").GetString());
        Assert.Contains(
            "Clean Architecture",
            root.GetProperty("info").GetProperty("description").GetString(),
            StringComparison.Ordinal);

        var tags = root.GetProperty("tags")
            .EnumerateArray()
            .Select(tag => tag.GetProperty("name").GetString())
            .ToArray();
        Assert.Contains("Authentication", tags);
        Assert.Contains("Orders", tags);

        var bearer = root.GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());

        var paths = root.GetProperty("paths");
        var login = paths.GetProperty("/auth/login").GetProperty("post");
        Assert.Equal("Login", login.GetProperty("operationId").GetString());
        Assert.False(login.TryGetProperty("security", out var loginSecurity) && loginSecurity.GetArrayLength() > 0);
        AssertResponses(login, "200", "400", "401", "500");
        AssertProblemContent(login, "400", "401", "500");
        Assert.Equal("dev@martech.com", GetJsonRequestExample(login).GetProperty("email").GetString());
        Assert.Equal("<jwt>", GetJsonResponseExample(login, "200").GetProperty("accessToken").GetString());

        var createOrder = paths.GetProperty("/api/orders").GetProperty("post");
        AssertOperationRequiresBearer(createOrder);
        AssertResponses(createOrder, "201", "400", "401", "500");
        AssertProblemContent(createOrder, "400", "401", "500");
        Assert.Equal(
            "Keyboard",
            GetJsonRequestExample(createOrder).GetProperty("items")[0].GetProperty("productName").GetString());
        Assert.Equal(375.50m, GetJsonResponseExample(createOrder, "201").GetProperty("totalAmount").GetDecimal());

        var listOrders = paths.GetProperty("/api/orders").GetProperty("get");
        AssertOperationRequiresBearer(listOrders);
        AssertResponses(listOrders, "200", "400", "401", "500");
        AssertProblemContent(listOrders, "400", "401", "500");
        AssertParameterSchema(listOrders, "page", "integer", defaultValue: 1, minimum: 1);
        AssertParameterSchema(listOrders, "pageSize", "integer", defaultValue: 10, minimum: 1, maximum: 100);
        Assert.Equal(10, GetJsonResponseExample(listOrders, "200").GetProperty("pageSize").GetInt32());

        var getOrderById = paths.GetProperty("/api/orders/{id}").GetProperty("get");
        AssertOperationRequiresBearer(getOrderById);
        AssertResponses(getOrderById, "200", "400", "401", "404", "500");
        AssertProblemContent(getOrderById, "400", "401", "404", "500");
        AssertParameterSchema(getOrderById, "id", "string", format: "uuid");

        var cancelOrder = paths.GetProperty("/api/orders/{id}/cancel").GetProperty("patch");
        AssertOperationRequiresBearer(cancelOrder);
        AssertResponses(cancelOrder, "200", "400", "401", "404", "409", "500");
        AssertProblemContent(cancelOrder, "400", "401", "404", "409", "500");
        AssertParameterSchema(cancelOrder, "id", "string", format: "uuid");
        Assert.Equal("Cancelled", GetJsonResponseExample(cancelOrder, "200").GetProperty("status").GetString());
    }

    [Fact]
    public async Task SwaggerUi_InDevelopment_LoadsOpenApiDocument()
    {
        using var factory = new OpenApiFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var uiScript = await client.GetStringAsync("/swagger/index.js");
        Assert.Contains("/openapi/v1.json", uiScript, StringComparison.Ordinal);
        Assert.Contains("EcommerceApi v1", uiScript, StringComparison.Ordinal);
        Assert.Contains("\"persistAuthorization\":false", uiScript, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_InDevelopment_RedirectsToSwaggerUi()
    {
        using var factory = new OpenApiFactory("Development");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task DocumentationEndpoints_InProductionDefault_AreNotExposed()
    {
        using var factory = new OpenApiFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var swaggerResponse = await client.GetAsync("/swagger");
        var rootResponse = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, openApiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, swaggerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rootResponse.StatusCode);
    }

    [Fact]
    public async Task DocumentationEndpoints_InProductionWithOpenApiEnabled_AreExposed()
    {
        using var factory = new OpenApiFactory("Production", openApiEnabled: true);
        using var client = factory.CreateClient();

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var swaggerResponse = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);
    }

    private static JsonElement GetJsonRequestExample(JsonElement operation) =>
        operation
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");

    private static JsonElement GetJsonResponseExample(JsonElement operation, string statusCode) =>
        operation
            .GetProperty("responses")
            .GetProperty(statusCode)
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");

    private static void AssertResponses(JsonElement operation, params string[] statusCodes)
    {
        var responses = operation.GetProperty("responses");
        foreach (var statusCode in statusCodes)
        {
            Assert.True(responses.TryGetProperty(statusCode, out _), $"Expected response {statusCode}.");
        }
    }

    private static void AssertProblemContent(JsonElement operation, params string[] statusCodes)
    {
        var responses = operation.GetProperty("responses");
        foreach (var statusCode in statusCodes)
        {
            Assert.True(
                responses
                    .GetProperty(statusCode)
                    .GetProperty("content")
                    .TryGetProperty("application/problem+json", out _),
                $"Expected application/problem+json for response {statusCode}.");
        }
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
        int? defaultValue = null,
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

        if (defaultValue is not null)
        {
            Assert.Equal(defaultValue.Value, schema.GetProperty("default").GetInt32());
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

    private sealed class OpenApiFactory : WebApplicationFactory<Program>
    {
        private const string SigningKey = "test-signing-key-with-at-least-32-bytes";

        private readonly string _environment;
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"ecommerceapi-openapi-{Guid.NewGuid():N}.db");

        private readonly Dictionary<string, string?> _previousEnvironmentValues = [];

        public OpenApiFactory(string environment, bool? openApiEnabled = null)
        {
            _environment = environment;

            SetEnvironmentVariable("ConnectionStrings__Orders", $"Data Source={_databasePath}");
            SetEnvironmentVariable("Jwt__Issuer", "EcommerceApi");
            SetEnvironmentVariable("Jwt__Audience", "EcommerceApi.Client");
            SetEnvironmentVariable("Jwt__LifetimeMinutes", "60");
            SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
            SetEnvironmentVariable("OpenApi__Enabled", openApiEnabled?.ToString());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
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

        private void SetEnvironmentVariable(string key, string? value)
        {
            _previousEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

[CollectionDefinition("OpenApi", DisableParallelization = true)]
public sealed class OpenApiCollection
{
}
