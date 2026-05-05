using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record UserDeactivated(
    UserId UserId,
    UserStatus PreviousStatus,
    string Reason,
    DateTimeOffset OccurredAt
) : IDomainEvent;