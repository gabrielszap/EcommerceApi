using EcommerceApi.Application.Orders.CreateOrder;
using EcommerceApi.Application.Orders.CancelOrder;
using EcommerceApi.Application.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace EcommerceApi.Api.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapPost("", CreateAsync)
            .WithName("CreateOrder")
            .WithSummary("Creates an order.")
            .WithDescription("Creates a pending order from caller-supplied items. Product, stock, payment, and discount flows are outside this feature.")
            .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireOpenApiBearerToken();

        group.MapGet("", GetPageAsync)
            .WithName("GetOrders")
            .WithSummary("Lists orders.")
            .WithDescription("Returns a deterministic newest-first page of persisted orders.")
            .Produces<PagedOrdersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireOpenApiBearerToken()
            .DescribeOpenApiPagination();

        group.MapGet("{id}", GetByIdAsync)
            .WithName("GetOrderById")
            .WithSummary("Gets one order.")
            .WithDescription("Returns a persisted order and its items by identifier.")
            .Produces<OrderDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireOpenApiBearerToken()
            .DescribeOpenApiOrderId();

        group.MapPatch("{id}/cancel", CancelAsync)
            .WithName("CancelOrder")
            .WithSummary("Cancels an order.")
            .WithDescription("Cancels a pending order. Confirmed or already cancelled orders return a conflict.")
            .Produces<OrderDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireOpenApiBearerToken()
            .DescribeOpenApiOrderId();

        return endpoints;
    }

    private static async Task<Results<Ok<OrderDetailsResponse>, ProblemHttpResult>> CancelAsync(
        string id,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid order identifier.",
                Detail = "The order identifier must be a GUID.",
                Type = "https://httpstatuses.com/400",
                Instance = httpContext.Request.Path
            });
        }

        var result = await sender.Send(new CancelOrderCommand(orderId), cancellationToken);
        return result.Outcome switch
        {
            CancelOrderOutcome.Cancelled => TypedResults.Ok(ToResponse(result.Order!)),
            CancelOrderOutcome.NotFound => TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order not found.",
                Detail = $"Order '{orderId}' was not found.",
                Type = "https://httpstatuses.com/404",
                Instance = httpContext.Request.Path
            }),
            CancelOrderOutcome.InvalidState => TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Order cannot be cancelled.",
                Detail = result.Detail,
                Type = "https://httpstatuses.com/409",
                Instance = httpContext.Request.Path
            }),
            _ => throw new InvalidOperationException($"Unsupported cancellation outcome '{result.Outcome}'.")
        };
    }

    private static OrderDetailsResponse ToResponse(OrderDetailsResult result) =>
        new(
            result.Id,
            result.CustomerId,
            result.Status.ToString(),
            result.CreatedAt,
            result.Items
                .Select(item => new OrderItemResponse(
                    item.Id,
                    item.OrderId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray(),
            result.TotalAmount);

    private static async Task<Created<CreateOrderResponse>> CreateAsync(
        CreateOrderRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateOrderCommand(
                request.CustomerId,
                request.Items?
                    .Select(item => item is null
                        ? null
                        : new CreateOrderItemCommand(
                            item.ProductName,
                            item.Quantity,
                            item.UnitPrice))
                    .ToArray()),
            cancellationToken);

        var response = new CreateOrderResponse(
            result.Id,
            result.CustomerId,
            result.Status.ToString(),
            result.CreatedAt,
            result.Items
                .Select(item => new CreateOrderItemResponse(
                    item.Id,
                    item.OrderId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray(),
            result.TotalAmount);

        return TypedResults.Created($"/api/orders/{response.Id}", response);
    }

    private static async Task<Results<Ok<PagedOrdersResponse>, ProblemHttpResult>> GetPageAsync(
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        string? page = null,
        string? pageSize = null)
    {
        if (!TryParsePositiveInt(page, 1, out var parsedPage) ||
            !TryParsePositiveInt(pageSize, 10, out var parsedPageSize))
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid pagination parameters.",
                Detail = "The page and pageSize query parameters must be positive integers.",
                Type = "https://httpstatuses.com/400",
                Instance = httpContext.Request.Path
            });
        }

        var result = await sender.Send(new GetOrdersQuery(parsedPage, parsedPageSize), cancellationToken);

        return TypedResults.Ok(new PagedOrdersResponse(
            result.Items
                .Select(order => new OrderSummaryResponse(
                    order.Id,
                    order.CustomerId,
                    order.Status.ToString(),
                    order.CreatedAt,
                    order.ItemCount,
                    order.TotalAmount))
                .ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage));
    }

    private static async Task<Results<Ok<OrderDetailsResponse>, ProblemHttpResult>> GetByIdAsync(
        string id,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid order identifier.",
                Detail = "The order identifier must be a GUID.",
                Type = "https://httpstatuses.com/400",
                Instance = httpContext.Request.Path
            });
        }

        var result = await sender.Send(new GetOrderByIdQuery(orderId), cancellationToken);
        if (result is null)
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order not found.",
                Detail = $"Order '{orderId}' was not found.",
                Type = "https://httpstatuses.com/404",
                Instance = httpContext.Request.Path
            });
        }

        return TypedResults.Ok(new OrderDetailsResponse(
            result.Id,
            result.CustomerId,
            result.Status.ToString(),
            result.CreatedAt,
            result.Items
                .Select(item => new OrderItemResponse(
                    item.Id,
                    item.OrderId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray(),
            result.TotalAmount));
    }

    private static bool TryParsePositiveInt(string? value, int defaultValue, out int parsedValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = defaultValue;
            return true;
        }

        return int.TryParse(value, out parsedValue) && parsedValue > 0;
    }

    private static RouteHandlerBuilder RequireOpenApiBearerToken(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, context, _) =>
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = []
            });

            return Task.CompletedTask;
        });

    private static RouteHandlerBuilder DescribeOpenApiPagination(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            SetIntegerParameterBounds(operation, "page", minimum: "1");
            SetIntegerParameterBounds(operation, "pageSize", minimum: "1", maximum: "100");
            return Task.CompletedTask;
        });

    private static RouteHandlerBuilder DescribeOpenApiOrderId(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var idParameter = operation.Parameters?.FirstOrDefault(parameter => parameter.Name == "id");
            if (idParameter?.Schema is OpenApiSchema schema)
            {
                schema.Type = JsonSchemaType.String;
                schema.Format = "uuid";
            }

            return Task.CompletedTask;
        });

    private static void SetIntegerParameterBounds(
        OpenApiOperation operation,
        string parameterName,
        string minimum,
        string? maximum = null)
    {
        var parameter = operation.Parameters?.FirstOrDefault(parameter => parameter.Name == parameterName);
        if (parameter?.Schema is not OpenApiSchema schema)
        {
            return;
        }

        schema.Type = JsonSchemaType.Integer;
        schema.Minimum = minimum;
        schema.Maximum = maximum;
    }
}
