using EcommerceApi.Application.Orders.CancelOrder;
using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Tests.Application.Orders;

public sealed class CancelOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithPendingOrder_CancelsPersistsAndReturnsOrder()
    {
        var order = CreateOrder();
        var writer = new CapturingOrderWriter(order);
        var handler = new CancelOrderCommandHandler(writer);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.Cancelled, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(OrderStatus.Cancelled, result.Order.Status);
        Assert.Equal(100.00m, result.Order.TotalAmount);
        Assert.True(writer.Saved);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task Handle_WithMissingOrder_ReturnsNotFoundAndDoesNotPersist()
    {
        var writer = new CapturingOrderWriter(null);
        var handler = new CancelOrderCommandHandler(writer);

        var result = await handler.Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.NotFound, result.Outcome);
        Assert.Null(result.Order);
        Assert.False(writer.Saved);
    }

    [Fact]
    public async Task Handle_WithCancelledOrder_ReturnsInvalidStateAndDoesNotPersist()
    {
        var order = CreateOrder();
        order.Cancel();
        var writer = new CapturingOrderWriter(order);
        var handler = new CancelOrderCommandHandler(writer);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.InvalidState, result.Outcome);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.False(writer.Saved);
    }

    [Fact]
    public async Task Handle_WithConfirmedOrder_ReturnsInvalidStateAndDoesNotPersist()
    {
        var order = CreateOrder();
        order.Confirm();
        var writer = new CapturingOrderWriter(order);
        var handler = new CancelOrderCommandHandler(writer);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.InvalidState, result.Outcome);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.False(writer.Saved);
    }

    [Fact]
    public async Task Handle_WhenPersistenceDetectsConcurrentStateChange_ReturnsInvalidState()
    {
        var order = CreateOrder();
        var handler = new CancelOrderCommandHandler(new ConcurrentConflictOrderWriter(order));

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.InvalidState, result.Outcome);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    private static Order CreateOrder() =>
        Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Keyboard", 2, 50.00m)],
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));

    private sealed class CapturingOrderWriter(Order? order) : IOrderWriter
    {
        public bool Saved { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(order?.Id == id ? order : null);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ConcurrentConflictOrderWriter(Order order) : IOrderWriter
    {
        public Task AddAsync(Order order, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(order.Id == id ? order : null);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new OrderPersistenceConcurrencyException(
                "The order state changed before the cancellation could be persisted.",
                new InvalidOperationException("Concurrency conflict."));
    }
}
