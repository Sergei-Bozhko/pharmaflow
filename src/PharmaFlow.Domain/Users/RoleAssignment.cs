using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Users.Events;

namespace PharmaFlow.Domain.Users;

public sealed class RoleAssignment : AggregateRoot<RoleAssignmentId>
{
    public UserId UserId { get; private set; }
    public Role Role { get; private set; }
    public Scope Scope { get; private set; } = default!;
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public SignatureId AssignedBySignatureId { get; private set; }

    private RoleAssignment() { }

    private RoleAssignment(
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
        if (userId == UserId.Empty)
        {
            return Error.Validation(
                "role_assignment.user_id.required",
                "UserId should not be empty."
            );
        }

        if (scope is null)
        {
            return Error.Validation(
                "role_assignment.scope.required",
                "Scope must not be null.");
        }

        if (assignedBySignatureId == SignatureId.Empty)
        {
            return Error.Validation(
                "role_assignment.assigned_by_signature_id.required",
                "Signature must not be empty."
            );
        }

        var roleAssignment = new RoleAssignment(
            id,
            userId,
            role,
            scope,
            assignedBySignatureId
        )
        {
            AssignedAt = clock.UtcNow,
            EndedAt = null,
        };
        roleAssignment.Raise(new RoleAssigned(id, userId, role, assignedBySignatureId, clock.UtcNow));
        return roleAssignment;
    }

    public Result End(SignatureId endingSignatureId, IClock clock)
    {
        if (EndedAt != null)
        {
            return Error.Conflict(
                "role_assignment.transition.invalid",
                "Role assignment has already ended."
            );
        }

        if (endingSignatureId == SignatureId.Empty)
        {
            return Error.Validation(
                "role_assignment.ending_signature_id.required",
                "SignatureId should not be empty."
            );
        }

        var now = clock.UtcNow;
        EndedAt = now;
        Raise(new RoleAssignmentEnded(Id, endingSignatureId, now));
        return Result.Success();
    }
}