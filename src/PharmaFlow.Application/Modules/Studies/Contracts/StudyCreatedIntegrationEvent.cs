using Mediator;

namespace PharmaFlow.Application.Modules.Studies.Contracts;

public sealed record StudyCreatedIntegrationEvent(
    Guid StudyId,
    DateTimeOffset OccurredAt,
    int Version = 1
    ) : INotification;