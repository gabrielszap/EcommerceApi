namespace EcommerceApi.Api.Orders;

public sealed record CreateOrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedAt,
    IReadOnlyCollection<CreateOrderItemResponse> Items,
    decimal TotalAmount);

public sealed record CreateOrderItemResponse(
    Guid Id,
    Guid OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
