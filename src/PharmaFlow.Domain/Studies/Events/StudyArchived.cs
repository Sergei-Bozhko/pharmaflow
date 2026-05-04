using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Studies.Events;

public sealed record StudyArchived(
    StudyId StudyId,
    DateTimeOffset OccurredAt
) : IDomainEvent;