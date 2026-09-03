using MediatR;

namespace EcommerceApi.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid Id) : IRequest<CancelOrderResult>;
