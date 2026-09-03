using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Application.Orders.Queries;

public sealed record PagedOrdersResult(
    IReadOnlyCollection<OrderSummaryResult> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}

public sealed record OrderSummaryResult(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    DateTime CreatedAt,
    int ItemCount,
    decimal TotalAmount);

public sealed record OrderDetailsResult(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    DateTime CreatedAt,
    IReadOnlyCollection<OrderItemResult> Items,
    decimal TotalAmount)
{
    public static OrderDetailsResult From(Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status,
            order.CreatedAt,
            order.Items
                .Select(item => new OrderItemResult(
                    item.Id,
                    item.OrderId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray(),
            order.TotalAmount);
}

public sealed record OrderItemResult(
    Guid Id,
    Guid OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
