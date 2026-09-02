using EcommerceApi.Application.Orders.CreateOrder;
using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Common;
using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Tests.Application.Orders;

public sealed class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidOrder_PersistsOrderAndReturnsDomainCalculatedTotal()
    {
        var writer = new CapturingOrderWriter();
        var handler = new CreateOrderCommandHandler(writer);
        var customerId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateOrderCommand(
                customerId,
                [
                    new CreateOrderItemCommand("Keyboard", 2, 150.00m),
                    new CreateOrderItemCommand("Mouse", 3, 50.00m)
                ]),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(450.00m, result.TotalAmount);
        Assert.Equal(2, result.Items.Count);
        Assert.Same(writer.Order, writer.Orders.Single());
        Assert.Equal(result.TotalAmount, writer.Order!.TotalAmount);
    }

    [Fact]
    public async Task Handle_WhenDomainRejectsEmptyItems_DoesNotPersist()
    {
        var writer = new CapturingOrderWriter();
        var handler = new CreateOrderCommandHandler(writer);

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            handler.Handle(new CreateOrderCommand(Guid.NewGuid(), []), CancellationToken.None));

        Assert.Empty(writer.Orders);
    }

    [Fact]
    public async Task Handle_WhenPersistenceFails_PropagatesFailure()
    {
        var handler = new CreateOrderCommandHandler(new ThrowingOrderWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new CreateOrderCommand(
                    Guid.NewGuid(),
                    [new CreateOrderItemCommand("Keyboard", 1, 10.00m)]),
                CancellationToken.None));
    }

    private sealed class CapturingOrderWriter : IOrderWriter
    {
        public List<Order> Orders { get; } = [];

        public Order? Order => Orders.SingleOrDefault();

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingOrderWriter : IOrderWriter
    {
        public Task AddAsync(Order order, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Persistence failed.");
    }
}
