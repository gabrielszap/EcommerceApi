using EcommerceApi.Api.Authentication;
using EcommerceApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Tests.Api;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void AddJwtAuthentication_WithoutSigningKey_RejectsStartupConfiguration()
    {
        var configuration = BuildJwtConfiguration(signingKey: null);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJwtAuthentication(configuration));

        Assert.Contains("signing key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddJwtAuthentication_WithSigningKeyShorterThan32Bytes_RejectsStartupConfiguration()
    {
        var configuration = BuildJwtConfiguration("too-short");

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJwtAuthentication(configuration));
    }

    [Fact]
    public void AddJwtAuthentication_WithCompleteConfiguration_RegistersAuthentication()
    {
        var services = new ServiceCollection();

        var result = services.AddJwtAuthentication(BuildJwtConfiguration(new string('k', 32)));

        Assert.Same(services, result);
        Assert.Contains(services, descriptor => descriptor.ServiceType.Name == "IAuthenticationService");
    }

    [Fact]
    public async Task ApplyDatabaseMigrationsAsync_WithFreshDatabase_AppliesInitialMigration()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ecommerce-startup-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<OrderDbContext>(options => options.UseSqlite($"Data Source={path}"));
            await using var provider = services.BuildServiceProvider();

            await provider.ApplyDatabaseMigrationsAsync();

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            Assert.Contains("20260902145400_InitialOrderSchema", await context.Database.GetAppliedMigrationsAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyDatabaseMigrationsAsync_WhenMigrationFails_LogsCriticalAndRethrows()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"ecommerce-startup-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        try
        {
            var loggerProvider = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
            services.AddDbContext<OrderDbContext>(options => options.UseSqlite($"Data Source={directoryPath}"));
            await using var provider = services.BuildServiceProvider();

            await Assert.ThrowsAnyAsync<Exception>(() => provider.ApplyDatabaseMigrationsAsync());

            Assert.Contains(LogLevel.Critical, loggerProvider.Levels);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private static IConfiguration BuildJwtConfiguration(string? signingKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "EcommerceApi",
            ["Jwt:Audience"] = "EcommerceApi.Client",
            ["Jwt:LifetimeMinutes"] = "60",
            ["Jwt:SigningKey"] = signingKey
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<LogLevel> Levels { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Levels);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(List<LogLevel> levels) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => levels.Add(logLevel);
    }
}
