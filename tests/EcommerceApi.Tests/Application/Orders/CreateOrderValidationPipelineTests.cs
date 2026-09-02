using EcommerceApi.Application;
using EcommerceApi.Application.Orders.CreateOrder;
using EcommerceApi.Application.Orders.Persistence;
using EcommerceApi.Domain.Orders;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Tests.Application.Orders;

public sealed class CreateOrderValidationPipelineTests
{
    [Fact]
    public async Task Send_WithInvalidOrderShape_ThrowsValidationExceptionBeforeTheHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IOrderWriter, FailingOrderWriter>();
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(
                new CreateOrderCommand(
                    Guid.Empty,
                    [new CreateOrderItemCommand(null, 0, 0m)]),
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "CustomerId");
        Assert.Contains(exception.Errors, error => error.PropertyName == "Items[0].ProductName");
        Assert.Contains(exception.Errors, error => error.PropertyName == "Items[0].Quantity");
        Assert.Contains(exception.Errors, error => error.PropertyName == "Items[0].UnitPrice");
    }

    private sealed class FailingOrderWriter : IOrderWriter
    {
        public Task AddAsync(Order order, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The handler should not run for invalid input.");
    }
}
