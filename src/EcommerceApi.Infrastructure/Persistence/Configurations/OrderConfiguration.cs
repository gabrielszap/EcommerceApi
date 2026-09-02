using EcommerceApi.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceApi.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "Orders",
            tableBuilder => tableBuilder.HasCheckConstraint("CK_Orders_Status_Valid", "Status IN (0, 1, 2)"));
        builder.HasKey(order => order.Id);
        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.Status).HasConversion<int>().IsRequired();
        builder.Property(order => order.CreatedAt).IsRequired();
        builder.Ignore(order => order.Items);
        builder.Ignore(order => order.TotalAmount);

        builder.HasMany<OrderItem>("_items")
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
