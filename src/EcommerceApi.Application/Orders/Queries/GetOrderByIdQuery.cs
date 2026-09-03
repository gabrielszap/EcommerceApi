using MediatR;

namespace EcommerceApi.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailsResult?>;
