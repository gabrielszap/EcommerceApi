using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Infrastructure.Persistence;

public sealed class EfCoreOrderWriter(OrderDbContext dbContext) : IOrderWriter
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
