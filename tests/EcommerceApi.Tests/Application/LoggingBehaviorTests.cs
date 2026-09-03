using EcommerceApi.Application;
using EcommerceApi.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Tests.Application;

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestSucceeds_LogsTypeOutcomeAndDurationWithoutPayload()
    {
        var logger = new CapturingLogger<LoggingBehavior<SensitiveRequest, string>>();
        var behavior = new LoggingBehavior<SensitiveRequest, string>(logger);
        var request = new SensitiveRequest("dev@martech.com", "Senha@123", "jwt-value");

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", result);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains(nameof(SensitiveRequest), entry.Message);
        Assert.Contains("Success", entry.Message);
        Assert.Equal(nameof(SensitiveRequest), entry.GetStateValue("RequestType"));
        Assert.Equal("Success", entry.GetStateValue("Outcome"));
        Assert.True((long)entry.GetStateValue("DurationMs")! >= 0);
        Assert.DoesNotContain(request.Email, entry.Message);
        Assert.DoesNotContain(request.Password, entry.Message);
        Assert.DoesNotContain(request.AccessToken, entry.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestFails_LogsOutcomeAndRethrows()
    {
        var logger = new CapturingLogger<LoggingBehavior<SensitiveRequest, string>>();
        var behavior = new LoggingBehavior<SensitiveRequest, string>(logger);
        const string signingKey = "test-signing-key-with-at-least-32-bytes";
        var request = new SensitiveRequest("dev@martech.com", "Senha@123", "jwt-value");
        var expected = new InvalidOperationException(
            $"Failed with {request.Password}, {request.AccessToken}, and {signingKey}.");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                request,
                _ => throw expected,
                CancellationToken.None));

        Assert.Same(expected, exception);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal(nameof(SensitiveRequest), entry.GetStateValue("RequestType"));
        Assert.Equal(nameof(InvalidOperationException), entry.GetStateValue("Outcome"));
        Assert.True((long)entry.GetStateValue("DurationMs")! >= 0);
        Assert.Contains(nameof(SensitiveRequest), entry.Message);
        Assert.Contains(nameof(InvalidOperationException), entry.Message);
        Assert.DoesNotContain(request.Email, entry.Message);
        Assert.DoesNotContain(request.Password, entry.Message);
        Assert.DoesNotContain(request.AccessToken, entry.Message);
        Assert.DoesNotContain(signingKey, entry.Message);
    }

    [Fact]
    public void AddApplication_RegistersLoggingPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPipelineBehavior<,>) &&
            descriptor.ImplementationType == typeof(LoggingBehavior<,>));
    }

    private sealed record SensitiveRequest(
        string Email,
        string Password,
        string AccessToken) : IRequest<string>;

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var stateValues = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(value => value.Key, value => value.Value)
                : [];

            Entries.Add(new LogEntry(logLevel, formatter(state, exception), stateValues, exception));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> StateValues,
        Exception? Exception)
    {
        public object? GetStateValue(string key) => StateValues[key];
    }
}
