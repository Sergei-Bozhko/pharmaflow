using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record RoleAssigned(
    RoleAssignmentId RoleAssignmentId,
    UserId UserId,
    Role Role,
    SignatureId AssignedBySignatureId,
    DateTimeOffset OccurredAt
) : IDomainEvent;