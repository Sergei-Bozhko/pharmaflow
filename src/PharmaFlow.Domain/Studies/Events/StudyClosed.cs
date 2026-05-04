using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Studies.Events;

public sealed record StudyClosed(
    StudyId StudyId,
    string Reason,
    SignatureMeta Signature,
    DateTimeOffset OccurredAt
) : IDomainEvent;