using Mediator;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Studies.Events;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class InProcIntegrationEventDispatcher(IPublisher publisher) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(INotification notification, Guid messageId, CancellationToken cancellationToken) =>
        await publisher.Publish(notification, cancellationToken).AsTask();
}