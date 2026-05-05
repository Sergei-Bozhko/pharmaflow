using System.Text.RegularExpressions;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users;

public sealed partial class User : Entity<UserId>
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

    public User() { }

    public User(
        UserId id,
        string username,
        string email,
        string fullName,
        string? displayTitle,
        IClock clock
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
                "Username is invalid. It should:\n- begin with letter;\n-be between 3 and 40 chars."
            );
        }

        if (string.IsNullOrWhiteSpace(email) ||
            !email.Contains('@') ||
            email.Length > 256)
        {
            return Error.Validation(
                "user.email.invalid",
                "Email is invalid. It should:\n- has '@';\n-be less then 256 chars."
            );
        }

        if (string.IsNullOrWhiteSpace(fullName) ||
            username.Length > 200)
        {
            return Error.Validation(
                "user.full_name.invalid",
                "Full name should be less 200 chars."
            );
        }

        if (displayTitle is null ||
            (displayTitle is not null &&
            displayTitle.Length > 20))
        {
            return Error.Validation(
                "user.display_title.invalid",
                "Display title should be less 200 chars."
            );
        }

        var user = new User(
            id,
            username,
            email,
            fullName,
            displayTitle,
            clock
        )
        {
            Status = UserStatus.Invited,
            MfaEnrolled = false,
            FailedLoginCount = 0,
        };
        // user.Raise(new UserCreated)
        return user;
    }

    [GeneratedRegex("^[a-z0-9._-]\\{3,40}$")]
    private static partial Regex UserNameRegex();
}