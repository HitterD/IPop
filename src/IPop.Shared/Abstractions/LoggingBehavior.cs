using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IPop.Shared.Abstractions;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly Action<ILogger, string, Exception?> HandlingRequest =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1000, nameof(HandlingRequest)), "Handling {Request}");

    private static readonly Action<ILogger, string, long, Exception?> HandledRequest =
        LoggerMessage.Define<string, long>(LogLevel.Information, new EventId(1001, nameof(HandledRequest)), "Handled {Request} in {Ms}ms");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        HandlingRequest(logger, name, null);
        var response = await next();
        sw.Stop();
        HandledRequest(logger, name, sw.ElapsedMilliseconds, null);
        return response;
    }
}
