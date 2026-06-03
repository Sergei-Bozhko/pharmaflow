using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Sites.CreateSite;

public sealed record CreateSiteCommand(
    StudyId StudyId,
    string SiteNumber,
    string Name,
    string Country,
    UserId PrincipalInvestigatorUserId
) : IIdempotentAppCommand<SiteId>
{
    
}