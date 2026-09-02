using EcommerceApi.Domain.Common;

namespace EcommerceApi.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
        ProductName = string.Empty;
    }

    private OrderItem(Guid id, string productName, int quantity, decimal unitPrice)
    {
        Id = id;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string ProductName { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public static OrderItem Create(string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Order item quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainRuleViolationException("Order item unit price must be greater than zero.");
        }

        return new OrderItem(Guid.NewGuid(), productName, quantity, unitPrice);
    }

    internal void AssignToOrder(Guid orderId) => OrderId = orderId;
}
