using System.Formats.Asn1;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class LoggingDomainEventDispatcher(ILogger<LoggingDomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Log.Dispatched(logger, domainEvent.GetType().Name);
        return Task.CompletedTask;
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> DispatchedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, nameof(Dispatched)),
                "Outbox dispatch (placeholder): {EventType}"
            );

        public static void Dispatched(ILogger logger, string eventType) =>
            DispatchedMessage(logger, eventType, null);
    }
}