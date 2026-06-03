using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Sites.Contracts;

public sealed record CreateSiteDto(
    StudyId StudyId,
    string SiteNumber,
    string Name,
    string Country,
    UserId PrincipalInvestigatorUserId);