using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Sites.Events;

public sealed record SiteCreated(
    SiteId SiteId,
    StudyId StudyId,
    DateTimeOffset OccurredAt
) : IDomainEvent;