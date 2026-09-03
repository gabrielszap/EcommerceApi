using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcommerceApi.Infrastructure.Persistence;

public sealed class DesignTimeOrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite("Data Source=ecommerce-design.db")
            .Options;

        return new OrderDbContext(options);
    }
}
