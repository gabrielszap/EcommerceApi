using EcommerceApi.Application.Orders.Persistence;
using MediatR;

namespace EcommerceApi.Application.Orders.Queries;

public sealed class GetOrderByIdQueryHandler(IOrderReader orderReader)
    : IRequestHandler<GetOrderByIdQuery, OrderDetailsResult?>
{
    public Task<OrderDetailsResult?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken) =>
        orderReader.GetByIdAsync(request.Id, cancellationToken);
}
