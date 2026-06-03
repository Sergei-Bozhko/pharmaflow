using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Sites.Contracts;

public sealed record CreateSiteDto(
    Guid StudyId,
    string SiteNumber,
    string Name,
    string Country,
    Guid PrincipalInvestigatorUserId);