namespace PharmaFlow.Application.Modules.Sites.CreateSite.Internal;

public sealed record CreateSiteDto(
    Guid StudyId,
    string SiteNumber,
    string Name,
    string Country,
    Guid PrincipalInvestigatorUserId);