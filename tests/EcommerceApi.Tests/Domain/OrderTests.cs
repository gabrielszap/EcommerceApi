using EcommerceApi.Domain.Common;
using EcommerceApi.Domain.Orders;

namespace EcommerceApi.Tests.Domain;

public sealed class OrderTests
{
    [Fact]
    public void Create_WithItems_CreatesPendingOrderAndAssignsItemRelationship()
    {
        var customerId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);
        var item = OrderItem.Create("Keyboard", 2, 125.50m);

        var order = Order.Create(customerId, [item], createdAt);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(createdAt, order.CreatedAt);
        Assert.Single(order.Items);
        Assert.Equal(order.Id, item.OrderId);
    }

    [Fact]
    public void TotalAmount_WithMultipleItems_SumsUnitPriceTimesQuantity()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create("Keyboard", 2, 125.50m), OrderItem.Create("Mouse", 3, 50m)],
            DateTime.UtcNow);

        Assert.Equal(401.00m, order.TotalAmount);
    }

    [Fact]
    public void Create_WithNoItems_ThrowsDomainRuleViolation()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(Guid.NewGuid(), [], DateTime.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateItem_WithMissingProductName_ThrowsDomainRuleViolation(string? productName)
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            OrderItem.Create(productName!, 1, 100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateItem_WithNonPositiveQuantity_ThrowsDomainRuleViolation(int quantity)
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            OrderItem.Create("Keyboard", quantity, 100m));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void CreateItem_WithNonPositivePrice_ThrowsDomainRuleViolation(string unitPrice)
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            OrderItem.Create("Keyboard", 1, decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture)));
    }
}
