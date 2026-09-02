using EcommerceApi.Application.Orders.Queries;

namespace EcommerceApi.Application.Orders.Persistence;

public interface IOrderReader
{
    Task<PagedOrdersResult> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<OrderDetailsResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
