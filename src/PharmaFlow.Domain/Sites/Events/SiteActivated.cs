using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Sites.Events;

public sealed record SiteActivated(
    SiteId SiteId,
    SignatureMeta SponsorSignature,
    SignatureMeta InvestigatorSignature,
    DateTimeOffset OccurredAt
) : IDomainEvent;