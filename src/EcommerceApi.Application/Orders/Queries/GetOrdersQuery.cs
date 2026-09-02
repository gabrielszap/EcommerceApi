using MediatR;

namespace EcommerceApi.Application.Orders.Queries;

public sealed record GetOrdersQuery(int Page = 1, int PageSize = 10) : IRequest<PagedOrdersResult>;
