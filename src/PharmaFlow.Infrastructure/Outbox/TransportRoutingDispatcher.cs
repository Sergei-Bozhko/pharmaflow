using Mediator;

using PharmaFlow.Application.Common.Events;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class TransportRoutingDispatcher(
    InProcIntegrationEventDispatcher inProc,
    HttpIntegrationEventDispatcher http,
    OutboxOptions options
) : IIntegrationEventDispatcher
{
    public Task DispatchAsync(
        INotification notification,
        Guid messageId,
        CancellationToken cancellationToken) =>
        (options.Transport == OutboxOptions.IntegrationTransport.Http ?
            http : (IIntegrationEventDispatcher)inProc)
            .DispatchAsync(notification, messageId, cancellationToken);
}