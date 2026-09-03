using EcommerceApi.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceApi.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(
            "OrderItems",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "Quantity > 0");
                tableBuilder.HasCheckConstraint(
                    "CK_OrderItems_UnitPrice_Positive",
                    "CAST(UnitPrice AS NUMERIC) > 0");
            });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.OrderId).IsRequired();
        builder.Property(item => item.ProductName).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.HasIndex(item => item.OrderId);
    }
}
