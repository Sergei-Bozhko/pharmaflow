using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Common.Auth;

public class SystemCurrentUser : ICurrentUser
{
    public UserId UserId => UserId.Empty;

    public string RoleAtTime => "system"; // SPRINT-6: Replace with HttpContext-backed identity.
}