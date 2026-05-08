using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Infrastructure.Auth;

public class SystemCurrentUser : ICurrentUser
{
    public UserId UserId => UserId.System;

    public string RoleAtTime => "system"; // SPRINT-6: Replace with HttpContext-backed identity.
}