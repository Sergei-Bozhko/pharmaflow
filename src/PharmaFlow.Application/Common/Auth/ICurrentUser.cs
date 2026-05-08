using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Common.Auth;

public interface ICurrentUser
{
    UserId UserId { get; }
    string RoleAtTime { get; }
}