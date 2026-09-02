using EcommerceApi.Application.Orders.Persistence;
using MediatR;

namespace EcommerceApi.Application.Orders.Queries;

public sealed class GetOrdersQueryHandler(IOrderReader orderReader)
    : IRequestHandler<GetOrdersQuery, PagedOrdersResult>
{
    public Task<PagedOrdersResult> Handle(GetOrdersQuery request, CancellationToken cancellationToken) =>
        orderReader.GetPageAsync(request.Page, request.PageSize, cancellationToken);
}
