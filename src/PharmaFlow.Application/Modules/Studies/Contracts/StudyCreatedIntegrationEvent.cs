using Mediator;

namespace PharmaFlow.Application.Modules.Studies.Contracts;

public sealed record StudyCreatedIntegrationEvent(Guid StudyId, DateTimeOffset OccurredAt) : INotification;