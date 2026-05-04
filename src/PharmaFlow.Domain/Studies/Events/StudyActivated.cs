using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Studies.Events;

public sealed record StudyActivated(
    StudyId StudyId,
    SignatureMeta Signature,
    DateTimeOffset OccurredAt
) : IDomainEvent;