using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Users;
using PharmaFlow.Domain.Users.Events;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Users;

public class UserTests
{
    private static readonly FrozenClock Clock = new(
        new DateTimeOffset(2026, 5, 5, 14, 0, 0, TimeSpan.Zero)
    );

    private static User NewValidUser(UserId? id = null) =>
        User.Create(
            id ?? UserId.New(),
            username: "alice.smith",
            email: "alice@example.com",
            fullName: "Alice Smith",
            displayTitle: "MD",
            clock: Clock
        ).Value;

    private static User UserInActive()
    {
        var u = NewValidUser();
        u.Activate(Clock);
        return u;
    }

    private static User UserInLocked()
    {
        var u = UserInActive();
        u.Lock("suspicious activity", Clock);
        return u;
    }

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_status_Invited()
    {
        var result = User.Create(
            UserId.New(), "alice.smith", "alice@example.com", "Alice Smith", "MD", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Invited, result.Value.Status);
        Assert.False(result.Value.MfaEnrolled);
        Assert.Equal(0, result.Value.FailedLoginCount);
    }

    [Fact]
    public void Create_raises_UserCreated_event()
    {
        var u = NewValidUser();

        Assert.Single(u.DomainEvents);
        Assert.IsType<UserCreated>(u.DomainEvents[0]);
    }

    [Fact]
    public void Create_accepts_null_DisplayTitle()
    {
        var result = User.Create(
            UserId.New(), "bob.jones", "bob@example.com", "Bob Jones", displayTitle: null, clock: Clock);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.DisplayTitle);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_uppercase_Username()
    {
        var result = User.Create(
            UserId.New(), "Alice.Smith", "alice@example.com", "Alice Smith", "MD", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("user.username.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_short_Username()
    {
        var result = User.Create(
            UserId.New(), "ab", "alice@example.com", "Alice Smith", "MD", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("user.username.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_invalid_Email_no_at_sign()
    {
        var result = User.Create(
            UserId.New(), "alice.smith", "alice-at-example.com", "Alice Smith", "MD", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("user.email.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_FullName()
    {
        var result = User.Create(
            UserId.New(), "alice.smith", "alice@example.com", "  ", "MD", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("user.full_name.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_long_DisplayTitle()
    {
        var result = User.Create(
            UserId.New(), "alice.smith", "alice@example.com", "Alice Smith",
            displayTitle: new string('X', 21), clock: Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("user.display_title.invalid", result.Error.Code);
    }

    // --- Lifecycle: happy path ---

    [Fact]
    public void Activate_from_Invited_transitions_to_Active()
    {
        var u = NewValidUser();
        u.ClearEvents();

        var result = u.Activate(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Active, u.Status);
        Assert.Single(u.DomainEvents);
        Assert.IsType<UserActivated>(u.DomainEvents[0]);
    }

    [Fact]
    public void Lock_from_Active_transitions_to_Locked()
    {
        var u = UserInActive();
        u.ClearEvents();

        var result = u.Lock("suspicious activity", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Locked, u.Status);
        Assert.Single(u.DomainEvents);
        Assert.IsType<UserLocked>(u.DomainEvents[0]);
    }

    [Fact]
    public void Unlock_from_Locked_transitions_to_Active()
    {
        var u = UserInLocked();
        u.ClearEvents();

        var result = u.Unlock(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Active, u.Status);
        Assert.Single(u.DomainEvents);
        Assert.IsType<UserUnlocked>(u.DomainEvents[0]);
    }

    [Fact]
    public void Deactivate_from_Active_transitions_to_Deactivated()
    {
        var u = UserInActive();
        u.ClearEvents();

        var result = u.Deactivate("offboarded", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Deactivated, u.Status);
        Assert.Single(u.DomainEvents);
        Assert.IsType<UserDeactivated>(u.DomainEvents[0]);
    }

    [Fact]
    public void Deactivate_from_Locked_transitions_to_Deactivated()
    {
        var u = UserInLocked();
        u.ClearEvents();

        var result = u.Deactivate("offboarded", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Deactivated, u.Status);
        Assert.Single(u.DomainEvents);
        Assert.IsType<UserDeactivated>(u.DomainEvents[0]);
    }

    [Fact]
    public void EnrolMfa_from_Active_sets_MfaEnrolled_true()
    {
        var u = UserInActive();
        u.ClearEvents();

        var result = u.EnrolMfa(Clock);

        Assert.True(result.IsSuccess);
        Assert.True(u.MfaEnrolled);
        Assert.Single(u.DomainEvents);
        Assert.IsType<UserMfaEnrolled>(u.DomainEvents[0]);
    }

    // --- Lifecycle: illegal transitions (assert ErrorType + Error.Code) ---

    [Fact]
    public void Lock_from_Invited_returns_Conflict()
    {
        var u = NewValidUser();

        var result = u.Lock("attempt", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("user.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void EnrolMfa_from_Invited_returns_Conflict()
    {
        var u = NewValidUser();

        var result = u.EnrolMfa(Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("user.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void Deactivate_from_Invited_returns_Conflict()
    {
        var u = NewValidUser();

        var result = u.Deactivate("offboarded", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("user.transition.invalid", result.Error.Code);
    }

    // --- Lifecycle validation ---

    [Fact]
    public void Lock_with_empty_reason_returns_Validation()
    {
        var u = UserInActive();

        var result = u.Lock("  ", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }

    [Fact]
    public void Deactivate_with_empty_reason_returns_Validation()
    {
        var u = UserInActive();

        var result = u.Deactivate("  ", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }

    // --- Auth bookkeeping (no events, no invariants) ---

    [Fact]
    public void RecordSuccessfulLogin_sets_LastLoginAt_and_resets_FailedLoginCount()
    {
        var u = UserInActive();
        u.RecordFailedLogin();
        u.RecordFailedLogin();
        u.ClearEvents();

        u.RecordSuccessfulLogin(Clock);

        Assert.Equal(Clock.UtcNow, u.LastLoginAt);
        Assert.Equal(0, u.FailedLoginCount);
        Assert.Empty(u.DomainEvents);
    }

    [Fact]
    public void RecordFailedLogin_increments_FailedLoginCount()
    {
        var u = UserInActive();
        u.ClearEvents();

        u.RecordFailedLogin();
        u.RecordFailedLogin();
        u.RecordFailedLogin();

        Assert.Equal(3, u.FailedLoginCount);
        Assert.Empty(u.DomainEvents);
    }

    [Fact]
    public void RecordPasswordChange_sets_PasswordLastChangedAt()
    {
        var u = UserInActive();
        u.ClearEvents();

        u.RecordPasswordChange(Clock);

        Assert.Equal(Clock.UtcNow, u.PasswordLastChangedAt);
        Assert.Empty(u.DomainEvents);
    }
}