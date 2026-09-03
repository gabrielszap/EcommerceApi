using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Infrastructure.Persistence;

public sealed class EfCoreOrderWriter(OrderDbContext dbContext) : IOrderWriter
{
    public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include("_items")
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new OrderPersistenceConcurrencyException(
                "The order state changed before the cancellation could be persisted.",
                exception);
        }
    }
}
