using MediatR;

namespace EcommerceApi.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderItemCommand?>? Items) : IRequest<CreateOrderResult>;

public sealed record CreateOrderItemCommand(
    string? ProductName,
    int Quantity,
    decimal UnitPrice);
