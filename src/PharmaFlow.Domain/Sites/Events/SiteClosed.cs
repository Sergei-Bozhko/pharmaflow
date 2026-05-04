using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Sites.Events;

public sealed record SiteClosed(
    SiteId SiteId,
    string Reason,
    SignatureMeta Signature,
    DateTimeOffset OccurredAt
) : IDomainEvent;