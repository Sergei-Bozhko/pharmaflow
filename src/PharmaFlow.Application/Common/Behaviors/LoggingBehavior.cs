using System.Diagnostics;

using Mediator;

using Microsoft.Extensions.Logging;

using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestType"] = requestName,
        });

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next(message, cancellationToken);
            sw.Stop();

            if (response is Result r && r.IsFailure)
            {
                Log.Failed(_logger, requestName, sw.ElapsedMilliseconds, r.Error.Code);
            }
            else
            {
                Log.Succeeded(_logger, requestName, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Threw(_logger, ex, requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, long, Exception?> SucceededMessage =
            LoggerMessage.Define<string, long>(
                LogLevel.Information,
                new EventId(1, nameof(Succeeded)),
                "{RequestType} succeeded in {ElapsedMs}ms");

        private static readonly Action<ILogger, string, long, string, Exception?> FailedMessage =
            LoggerMessage.Define<string, long, string>(
                LogLevel.Warning,
                new EventId(2, nameof(Failed)),
                "{RequestType} failed in {ElapsedMs}ms: {ErrorCode}");

        private static readonly Action<ILogger, string, long, Exception?> ThrewMessage =
            LoggerMessage.Define<string, long>(
                LogLevel.Error,
                new EventId(3, nameof(Threw)),
                "{RequestType} threw after {ElapsedMs}ms");

        public static void Succeeded(ILogger logger, string requestType, long elapsedMs) =>
            SucceededMessage(logger, requestType, elapsedMs, null);

        public static void Failed(ILogger logger, string requestType, long elapsedMs, string errorCode) =>
            FailedMessage(logger, requestType, elapsedMs, errorCode, null);

        public static void Threw(ILogger logger, Exception ex, string requestType, long elapsedMs) =>
            ThrewMessage(logger, requestType, elapsedMs, ex);
    }

}