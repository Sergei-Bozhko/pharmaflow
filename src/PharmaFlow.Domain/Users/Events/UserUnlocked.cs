using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record UserUnlocked(
    UserId UserId,
    DateTimeOffset OccurredAt
) : IDomainEvent;