using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Application.Orders.Persistence;

public interface IOrderWriter
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
