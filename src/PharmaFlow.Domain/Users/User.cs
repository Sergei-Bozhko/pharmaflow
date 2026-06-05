using System.Text.RegularExpressions;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Users.Events;

namespace PharmaFlow.Domain.Users;

public sealed partial class User : AggregateRoot<UserId>
{
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string? DisplayTitle { get; private set; } = default!;
    public UserStatus Status { get; private set; }
    public bool MfaEnrolled { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? PasswordLastChangedAt { get; private set; } = default!;

    private User() { }

    private User(
        UserId id,
        string username,
        string email,
        string fullName,
        string? displayTitle
    ) : base(id)
    {
        Username = username;
        Email = email;
        FullName = fullName;
        DisplayTitle = displayTitle;
    }

    public static Result<User> Create(
        UserId id,
        string username,
        string email,
        string fullName,
        string? displayTitle,
        IClock clock)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            !UserNameRegex().IsMatch(username))
        {
            return Error.Validation(
                "user.username.invalid",
                "Username must be 3–40 characters, lowercase letters, digits, '.', '_', '-'."
            );
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1 ||
            email.Length > 256)
        {
            return Error.Validation(
                "user.email.invalid",
                "Email must contain '@' and be at most 256 characters."
            );
        }

        if (string.IsNullOrWhiteSpace(fullName) ||
            fullName.Length > 200)
        {
            return Error.Validation(
                "user.full_name.invalid",
                "Full name should be less than 200 chars."
            );
        }

        if (displayTitle is not null && displayTitle.Length > 20)
        {
            return Error.Validation(
                "user.display_title.invalid",
                "Display title should be less than 20 chars."
            );
        }

        var user = new User(
            id,
            username,
            email,
            fullName,
            displayTitle
        )
        {
            Status = UserStatus.Invited,
            MfaEnrolled = false,
            FailedLoginCount = 0,
        };
        user.Raise(new UserCreated(id, clock.UtcNow));
        return user;
    }

    public Result Activate(IClock clock)
    {
        if (Status != UserStatus.Invited)
        {
            return Error.Conflict(
                "user.transition.invalid",
                $"Cannot Activate a user with status {Status}."
            );
        }
        Status = UserStatus.Active;
        Raise(new UserActivated(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result Lock(string reason, IClock clock)
    {
        if (Status != UserStatus.Active)
        {
            return Error.Conflict(
                "user.transition.invalid",
                $"Cannot Lock a user with status {Status}."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "user.lock.reason_required",
                "Reason must be non-empty string."
            );
        }
        Status = UserStatus.Locked;
        Raise(new UserLocked(Id, reason, clock.UtcNow));
        return Result.Success();
    }

    public Result Unlock(IClock clock)
    {
        if (Status != UserStatus.Locked)
        {
            return Error.Conflict(
                "user.transition.invalid",
                $"Cannot Unlock a user with status {Status}."
            );
        }
        Status = UserStatus.Active;
        Raise(new UserUnlocked(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result Deactivate(string reason, IClock clock)
    {
        if (Status != UserStatus.Active &&
            Status != UserStatus.Locked)
        {
            return Error.Conflict(
                "user.transition.invalid",
                $"Cannot deactivate a user with status {Status}."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "user.deactivate.reason_required",
                "Reason must be non-empty string."
            );
        }
        var previousStatus = Status;
        Status = UserStatus.Deactivated;
        Raise(new UserDeactivated(Id, previousStatus, reason, clock.UtcNow));
        return Result.Success();
    }

    public Result EnrolMfa(IClock clock)
    {
        if (Status != UserStatus.Active)
        {
            return Error.Conflict(
                "user.transition.invalid",
                $"Cannot enrol MFA for a user with status {Status}."
            );
        }

        MfaEnrolled = true;
        Raise(new UserMfaEnrolled(Id, clock.UtcNow));
        return Result.Success();
    }

    public void RecordSuccessfulLogin(IClock clock)
    {
        LastLoginAt = clock.UtcNow;
        FailedLoginCount = 0;
    }

    public void RecordFailedLogin()
    {
        FailedLoginCount++;
    }

    public void RecordPasswordChange(IClock clock)
    {
        PasswordLastChangedAt = clock.UtcNow;
    }

    [GeneratedRegex("^[a-z0-9._-]{3,40}$")]
    private static partial Regex UserNameRegex();
}