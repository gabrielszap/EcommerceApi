using EcommerceApi.Application.Authentication;
using EcommerceApi.Infrastructure.Authentication;
using EcommerceApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Orders");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Orders must be configured.");
        }

        var sqliteConnectionString = new SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(sqliteConnectionString.DataSource) &&
            !string.Equals(sqliteConnectionString.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var databasePath = Path.GetFullPath(sqliteConnectionString.DataSource, Directory.GetCurrentDirectory());
            var databaseDirectory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrWhiteSpace(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            sqliteConnectionString.DataSource = databasePath;
        }

        services.AddDbContext<OrderDbContext>(options => options.UseSqlite(sqliteConnectionString.ToString()));
        return services;
    }

    public static IServiceCollection AddJwtAccessTokenGeneration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IAccessTokenGenerator>(
            new JwtAccessTokenGenerator(JwtTokenOptions.FromConfiguration(configuration)));
        return services;
    }
}
