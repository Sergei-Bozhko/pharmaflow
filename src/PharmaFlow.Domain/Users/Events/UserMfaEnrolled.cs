using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record UserMfaEnrolled(
    UserId UserId,
    DateTimeOffset OccurredAt
) : IDomainEvent;