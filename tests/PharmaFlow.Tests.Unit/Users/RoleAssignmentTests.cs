using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Users;
using PharmaFlow.Domain.Users.Events;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Users;

public class RoleAssignmentTests
{
    private static readonly FrozenClock Clock = new(
        new DateTimeOffset(2026, 5, 5, 14, 0, 0, TimeSpan.Zero)
    );

    private static RoleAssignment NewValidAssignment() =>
        RoleAssignment.Create(
            RoleAssignmentId.New(),
            userId: UserId.New(),
            role: Role.Investigator,
            scope: Scope.ForStudy(StudyId.New()),
            assignedBySignatureId: SignatureId.New(),
            clock: Clock
        ).Value;

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_AssignedAt_set_and_EndedAt_null()
    {
        var ra = NewValidAssignment();

        Assert.Equal(Clock.UtcNow, ra.AssignedAt);
        Assert.Null(ra.EndedAt);
    }

    [Fact]
    public void Create_raises_RoleAssigned_event()
    {
        var ra = NewValidAssignment();

        Assert.Single(ra.DomainEvents);
        Assert.IsType<RoleAssigned>(ra.DomainEvents[0]);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_empty_UserId()
    {
        var result = RoleAssignment.Create(
            RoleAssignmentId.New(), UserId.Empty, Role.Investigator,
            Scope.ForStudy(StudyId.New()), SignatureId.New(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("role_assignment.user_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_AssignedBySignatureId()
    {
        var result = RoleAssignment.Create(
            RoleAssignmentId.New(), UserId.New(), Role.Investigator,
            Scope.ForStudy(StudyId.New()), SignatureId.Empty, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("role_assignment.assigned_by_signature_id.required", result.Error.Code);
    }

    // --- Lifecycle: End ---

    [Fact]
    public void End_sets_EndedAt_and_raises_RoleAssignmentEnded()
    {
        var ra = NewValidAssignment();
        ra.ClearEvents();

        var result = ra.End(SignatureId.New(), Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(Clock.UtcNow, ra.EndedAt);
        Assert.Single(ra.DomainEvents);
        Assert.IsType<RoleAssignmentEnded>(ra.DomainEvents[0]);
    }

    [Fact]
    public void End_when_already_ended_returns_Conflict()
    {
        var ra = NewValidAssignment();
        ra.End(SignatureId.New(), Clock);

        var result = ra.End(SignatureId.New(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("role_assignment.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void End_with_empty_SignatureId_returns_Validation()
    {
        var ra = NewValidAssignment();

        var result = ra.End(SignatureId.Empty, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }
}