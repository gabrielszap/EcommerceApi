using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Application.Orders.CreateOrder;

public sealed record CreateOrderResult(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    DateTime CreatedAt,
    IReadOnlyCollection<CreateOrderItemResult> Items,
    decimal TotalAmount)
{
    public static CreateOrderResult From(Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status,
            order.CreatedAt,
            order.Items
                .Select(item => new CreateOrderItemResult(
                    item.Id,
                    item.OrderId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray(),
            order.TotalAmount);
}

public sealed record CreateOrderItemResult(
    Guid Id,
    Guid OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
