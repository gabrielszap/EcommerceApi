using EcommerceApi.Application.Orders.Queries;

namespace EcommerceApi.Application.Orders.CancelOrder;

public sealed record CancelOrderResult(
    CancelOrderOutcome Outcome,
    OrderDetailsResult? Order,
    string? Detail)
{
    public static CancelOrderResult Cancelled(OrderDetailsResult order) =>
        new(CancelOrderOutcome.Cancelled, order, null);

    public static CancelOrderResult NotFound() =>
        new(CancelOrderOutcome.NotFound, null, null);

    public static CancelOrderResult InvalidState(string detail) =>
        new(CancelOrderOutcome.InvalidState, null, detail);
}

public enum CancelOrderOutcome
{
    Cancelled,
    NotFound,
    InvalidState
}
