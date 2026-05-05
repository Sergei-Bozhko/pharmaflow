using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users.Events;

public sealed record RoleAssigned(
    RoleAssignmentId RoleAssignmentId,
    SignatureId AssignedBy,
    DateTimeOffset OccurredAt
) : IDomainEvent;