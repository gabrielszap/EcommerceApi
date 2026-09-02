namespace EcommerceApi.Api.Orders;

public sealed record PagedOrdersResponse(
    IReadOnlyCollection<OrderSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record OrderSummaryResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedAt,
    int ItemCount,
    decimal TotalAmount);

public sealed record OrderDetailsResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedAt,
    IReadOnlyCollection<OrderItemResponse> Items,
    decimal TotalAmount);

public sealed record OrderItemResponse(
    Guid Id,
    Guid OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
