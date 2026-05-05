using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record UserLocked(
    UserId UserId,
    DateTimeOffset OccurredAt
) : IDomainEvent;