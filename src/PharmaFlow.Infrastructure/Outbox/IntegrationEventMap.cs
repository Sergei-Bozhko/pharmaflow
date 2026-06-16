using Mediator;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Studies.Events;

namespace PharmaFlow.Infrastructure.Outbox;

public static class IntegrationEventMap
{
    public static INotification? ToIntegrationEvent(IDomainEvent e) => e switch
    {
        StudyCreated s => new StudyCreatedIntegrationEvent(s.StudyId.Value, e.OccurredAt),
        _ => null,
    };
}