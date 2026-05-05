using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Domain.Users;

public sealed record Scope(ScopeKind Kind, StudyId? StudyId, SiteId? SiteId)
{
    public static Scope System() => new(ScopeKind.System, null, null);
    public static Scope ForStudy(StudyId studyId) => new(ScopeKind.Study, studyId, null);
    public static Scope ForSite(SiteId siteId) => new(ScopeKind.Site, null, siteId);
}