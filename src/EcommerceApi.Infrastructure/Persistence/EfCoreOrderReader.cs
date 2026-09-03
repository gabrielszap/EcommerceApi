using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Application.Orders.Queries;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Infrastructure.Persistence;

public sealed class EfCoreOrderReader(OrderDbContext dbContext) : IOrderReader
{
    public async Task<PagedOrdersResult> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await dbContext.Orders
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include("_items")
            .OrderByDescending(order => order.CreatedAt)
            .ThenBy(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedOrdersResult(
            orders
                .Select(order => new OrderSummaryResult(
                    order.Id,
                    order.CustomerId,
                    order.Status,
                    order.CreatedAt,
                    order.Items.Count,
                    order.TotalAmount))
                .ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<OrderDetailsResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include("_items")
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderDetailsResult(
            order.Id,
            order.CustomerId,
            order.Status,
            order.CreatedAt,
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResult(
                    item.Id,
                    item.OrderId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray(),
            order.TotalAmount);
    }
}
