using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Application.Orders.Queries;
using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Tests.Application.Orders;

public sealed class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingOrder_ReturnsOrderFromReader()
    {
        var orderId = Guid.NewGuid();
        var reader = new StubOrderReader(orderId);
        var handler = new GetOrderByIdQueryHandler(reader);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.Handle(new GetOrderByIdQuery(orderId), cancellationTokenSource.Token);

        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal(25.00m, result.TotalAmount);
        Assert.Equal(cancellationTokenSource.Token, reader.CancellationToken);
    }

    [Fact]
    public async Task Handle_WithMissingOrder_ReturnsNull()
    {
        var handler = new GetOrderByIdQueryHandler(new StubOrderReader(Guid.NewGuid()));

        var result = await handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubOrderReader(Guid existingOrderId) : IOrderReader
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task<PagedOrdersResult> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OrderDetailsResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;

            if (id != existingOrderId)
            {
                return Task.FromResult<OrderDetailsResult?>(null);
            }

            return Task.FromResult<OrderDetailsResult?>(new OrderDetailsResult(
                existingOrderId,
                Guid.NewGuid(),
                OrderStatus.Pending,
                new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
                [
                    new OrderItemResult(
                        Guid.NewGuid(),
                        existingOrderId,
                        "Keyboard",
                        2,
                        12.50m)
                ],
                25.00m));
        }
    }
}
