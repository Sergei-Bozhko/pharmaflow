using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record RoleAssignmentEnded(
    RoleAssignmentId RoleAssignmentId,
    SignatureId EndedBySignatureId,
    DateTimeOffset OccurredAt
) : IDomainEvent;