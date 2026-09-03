using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            logger.LogInformation(
                "MediatR request completed. RequestType: {RequestType}; Outcome: {Outcome}; DurationMs: {DurationMs}",
                requestName,
                "Success",
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogWarning(
                "MediatR request failed. RequestType: {RequestType}; Outcome: {Outcome}; DurationMs: {DurationMs}",
                requestName,
                exception.GetType().Name,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
