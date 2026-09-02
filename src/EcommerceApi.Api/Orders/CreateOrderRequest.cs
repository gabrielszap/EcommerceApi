namespace EcommerceApi.Api.Orders;

public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderItemRequest?>? Items);

public sealed record CreateOrderItemRequest(
    string? ProductName,
    int Quantity,
    decimal UnitPrice);
