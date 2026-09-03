using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Application.Orders.Queries;
using EcommerceApi.Domain.Common;
using MediatR;

namespace EcommerceApi.Application.Orders.CancelOrder;

public sealed class CancelOrderCommandHandler(IOrderWriter orderWriter)
    : IRequestHandler<CancelOrderCommand, CancelOrderResult>
{
    public async Task<CancelOrderResult> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderWriter.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (order is null)
        {
            return CancelOrderResult.NotFound();
        }

        try
        {
            order.Cancel();
        }
        catch (DomainRuleViolationException exception)
        {
            return CancelOrderResult.InvalidState(exception.Message);
        }

        try
        {
            await orderWriter.SaveChangesAsync(cancellationToken);
        }
        catch (OrderPersistenceConcurrencyException exception)
        {
            return CancelOrderResult.InvalidState(exception.Message);
        }

        return CancelOrderResult.Cancelled(OrderDetailsResult.From(order));
    }
}
