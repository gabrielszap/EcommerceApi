using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Orders;
using MediatR;

namespace EcommerceApi.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler(IOrderWriter orderWriter)
    : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var items = (request.Items ?? [])
            .Select(item => OrderItem.Create(item!.ProductName!, item.Quantity, item.UnitPrice))
            .ToArray();

        var order = Order.Create(request.CustomerId, items, DateTime.UtcNow);

        await orderWriter.AddAsync(order, cancellationToken);

        return CreateOrderResult.From(order);
    }
}
