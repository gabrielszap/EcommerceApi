using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EcommerceApi.Api.OpenApi;

public static class OpenApiEndpointExtensions
{
    private const string JsonContentType = "application/json";
    private const string ProblemJsonContentType = "application/problem+json";

    public static RouteHandlerBuilder RequireOpenApiBearerToken(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, context, _) =>
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = []
            });

            return Task.CompletedTask;
        });

    public static RouteHandlerBuilder WithJsonRequestExample(this RouteHandlerBuilder builder, string json) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.RequestBody?.Content?.TryGetValue(JsonContentType, out var mediaType) is true)
            {
                mediaType.Example = JsonNode.Parse(json);
            }

            return Task.CompletedTask;
        });

    public static RouteHandlerBuilder WithJsonResponseExample(
        this RouteHandlerBuilder builder,
        int statusCode,
        string json) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var mediaType = GetResponseMediaType(operation, statusCode.ToString(), JsonContentType);
            if (mediaType is not null)
            {
                mediaType.Example = JsonNode.Parse(json);
            }

            return Task.CompletedTask;
        });

    public static RouteHandlerBuilder WithProblemDetailsExamples(
        this RouteHandlerBuilder builder,
        params int[] statusCodes) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            foreach (var statusCode in statusCodes)
            {
                var mediaType = GetResponseMediaType(operation, statusCode.ToString(), ProblemJsonContentType);
                if (mediaType is not null)
                {
                    mediaType.Example = JsonNode.Parse(CreateProblemExample(statusCode));
                }
            }

            return Task.CompletedTask;
        });

    public static RouteHandlerBuilder DescribeOpenApiPagination(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            SetIntegerParameterBounds(
                operation,
                "page",
                description: "1-based page number. Defaults to 1.",
                minimum: "1",
                defaultValue: "1");
            SetIntegerParameterBounds(
                operation,
                "pageSize",
                description: "Orders per page. Defaults to 10 and cannot exceed 100.",
                minimum: "1",
                maximum: "100",
                defaultValue: "10");

            return Task.CompletedTask;
        });

    public static RouteHandlerBuilder DescribeOpenApiOrderId(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var idParameter = operation.Parameters?.FirstOrDefault(parameter => parameter.Name == "id");
            if (idParameter?.Schema is OpenApiSchema schema)
            {
                idParameter.Description = "Order identifier as a non-empty UUID.";
                schema.Type = JsonSchemaType.String;
                schema.Format = "uuid";
            }

            return Task.CompletedTask;
        });

    private static OpenApiMediaType? GetResponseMediaType(
        OpenApiOperation operation,
        string statusCode,
        string contentType)
    {
        if (operation.Responses is null ||
            !operation.Responses.TryGetValue(statusCode, out var response) ||
            response.Content is null)
        {
            return null;
        }

        if (response.Content.TryGetValue(contentType, out var exactMediaType))
        {
            return exactMediaType;
        }

        return response.Content.FirstOrDefault(entry =>
            string.Equals(entry.Key, contentType, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string CreateProblemExample(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => """
                {
                  "type": "https://httpstatuses.com/400",
                  "title": "One or more validation errors occurred.",
                  "status": 400,
                  "detail": "Safe client-facing detail when applicable.",
                  "instance": "/api/orders",
                  "errors": {
                    "Items[0].Quantity": [
                      "'Quantity' must be greater than '0'."
                    ]
                  }
                }
                """,
            StatusCodes.Status401Unauthorized => """
                {
                  "type": "https://httpstatuses.com/401",
                  "title": "Unauthorized.",
                  "status": 401,
                  "detail": "A valid bearer token is required.",
                  "instance": "/api/orders"
                }
                """,
            StatusCodes.Status404NotFound => """
                {
                  "type": "https://httpstatuses.com/404",
                  "title": "Order not found.",
                  "status": 404,
                  "detail": "Order was not found.",
                  "instance": "/api/orders/22222222-2222-2222-2222-222222222222"
                }
                """,
            StatusCodes.Status409Conflict => """
                {
                  "type": "https://httpstatuses.com/409",
                  "title": "Order cannot be cancelled.",
                  "status": 409,
                  "detail": "Only pending orders can be cancelled.",
                  "instance": "/api/orders/22222222-2222-2222-2222-222222222222/cancel"
                }
                """,
            StatusCodes.Status500InternalServerError => """
                {
                  "title": "An unexpected error occurred.",
                  "status": 500,
                  "traceId": "00-00000000000000000000000000000000-0000000000000000-00"
                }
                """,
            _ => $$"""
                {
                  "title": "HTTP {{statusCode}} response.",
                  "status": {{statusCode}}
                }
                """
        };

    private static void SetIntegerParameterBounds(
        OpenApiOperation operation,
        string parameterName,
        string description,
        string minimum,
        string? maximum = null,
        string? defaultValue = null)
    {
        var parameter = operation.Parameters?.FirstOrDefault(parameter => parameter.Name == parameterName);
        if (parameter?.Schema is not OpenApiSchema schema)
        {
            return;
        }

        parameter.Description = description;
        schema.Type = JsonSchemaType.Integer;
        schema.Minimum = minimum;
        schema.Maximum = maximum;
        schema.Default = defaultValue is null ? null : JsonNode.Parse(defaultValue);
    }
}
