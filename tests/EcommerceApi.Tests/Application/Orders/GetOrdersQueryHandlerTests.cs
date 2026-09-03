using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Application.Orders.Queries;
using EcommerceApi.Domain.Orders;
using FluentValidation;

namespace EcommerceApi.Tests.Application.Orders;

public sealed class GetOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithDefaultPagination_ReturnsReaderPage()
    {
        var reader = new CapturingOrderReader();
        var handler = new GetOrdersQueryHandler(reader);

        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(1, reader.Page);
        Assert.Equal(10, reader.PageSize);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task Handle_WithExplicitPagination_PropagatesPaginationAndCancellationToken()
    {
        var reader = new CapturingOrderReader();
        var handler = new GetOrdersQueryHandler(reader);
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.Handle(new GetOrdersQuery(2, 5), cancellationTokenSource.Token);

        Assert.Equal(2, reader.Page);
        Assert.Equal(5, reader.PageSize);
        Assert.Equal(cancellationTokenSource.Token, reader.CancellationToken);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(-1, 10)]
    [InlineData(1, -10)]
    public void Validate_WithInvalidPagination_ReturnsValidationFailure(int page, int pageSize)
    {
        var validator = new GetOrdersQueryValidator();

        var result = validator.Validate(new GetOrdersQuery(page, pageSize));

        Assert.False(result.IsValid);
    }

    private sealed class CapturingOrderReader : IOrderReader
    {
        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<PagedOrdersResult> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            Page = page;
            PageSize = pageSize;
            CancellationToken = cancellationToken;

            return Task.FromResult(new PagedOrdersResult(
                [
                    new OrderSummaryResult(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        OrderStatus.Pending,
                        new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
                        1,
                        10.00m)
                ],
                page,
                pageSize,
                3));
        }

        public Task<OrderDetailsResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
