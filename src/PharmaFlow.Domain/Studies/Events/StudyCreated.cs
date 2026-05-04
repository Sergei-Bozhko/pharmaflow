using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Studies.Events;

public sealed record StudyCreated(StudyId StudyId, DateTimeOffset OccurredAt) : IDomainEvent;