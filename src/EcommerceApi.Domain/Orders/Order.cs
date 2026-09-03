using EcommerceApi.Domain.Common;

namespace EcommerceApi.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    private Order(Guid id, Guid customerId, DateTime createdAt, IEnumerable<OrderItem> items)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;

        foreach (var item in items)
        {
            item.AssignToOrder(id);
            _items.Add(item);
        }
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(item => item.UnitPrice * item.Quantity);

    public static Order Create(Guid customerId, IEnumerable<OrderItem> items, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(items);

        var materializedItems = items.ToList();
        if (materializedItems.Count == 0)
        {
            throw new DomainRuleViolationException("An order must contain at least one item.");
        }

        return new Order(Guid.NewGuid(), customerId, createdAt, materializedItems);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Only pending orders can be confirmed.");
        }

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Only pending orders can be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }
}
