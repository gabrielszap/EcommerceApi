namespace EcommerceApi.Application.Orders.Persistence;

public sealed class OrderPersistenceConcurrencyException : Exception
{
    public OrderPersistenceConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
