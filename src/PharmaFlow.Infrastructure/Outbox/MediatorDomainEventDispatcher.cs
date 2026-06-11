using Mediator;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Studies.Events;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class MediatorDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        INotification? integrationEvent = domainEvent switch
        {
            StudyCreated e => new StudyCreatedIntegrationEvent(e.StudyId.Value, e.OccurredAt),
            _ => null,
        };

        if (integrationEvent is not null)
        {
            await publisher.Publish(integrationEvent, cancellationToken);
        }
    }
}