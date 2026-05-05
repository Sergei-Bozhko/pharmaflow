using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users;

public sealed class RoleAssignment : Entity<RoleAssignmentId>
{
    public UserId UserId { get; private set; }
    public Role Role { get; private set; }
    public Scope Scope { get; private set; } = default!;
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public SignatureId AssignedBySignatureId { get; private set; }

    public RoleAssignment( ) { }

    public RoleAssignment(
        RoleAssignmentId id,
        UserId userId,
        Role role,
        Scope scope,
        SignatureId assignedBySignatureId
    ) : base(id)
    {
        UserId = userId;
        Role = role;
        Scope = scope;
        AssignedBySignatureId = assignedBySignatureId;
    }

    public static Result<RoleAssignment> Create(
        RoleAssignmentId id,
        UserId userId,
        Role role,
        Scope scope,
        SignatureId assignedBySignatureId,
        IClock clock)
    {
        var roleAssignment = new RoleAssignment(
            id,
            userId,
            role,
            scope,
            assignedBySignatureId
        )
        {
            AssignedAt = clock.UtcNow,
        };
        // roleAssignment.Raise();
        return roleAssignment;
    }
}