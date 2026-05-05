using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Users;

public sealed record Scope(ScopeKind Kind, StudyId? StudyId, SiteId? SiteId)
{
    public ScopeKind Kind { get; init; } = ValidateAndReturn(Kind, StudyId, SiteId);

    public static Scope System() => new(ScopeKind.System, null, null);
    public static Scope ForStudy(StudyId studyId) => new(ScopeKind.Study, studyId, null);
    public static Scope ForSite(SiteId siteId) => new(ScopeKind.Site, null, siteId);

    private static ScopeKind ValidateAndReturn(ScopeKind kind, StudyId? studyId, SiteId? siteId)
    {
        var ok = kind switch
        {
            ScopeKind.System => studyId is null && siteId is null,
            ScopeKind.Study => studyId is not null && siteId is null,
            ScopeKind.Site => studyId is null && siteId is not null,
            _ => false,
        };
        if (!ok)
        {
            throw new InvalidOperationException(
                $"Inconsistent Scope: Kind={kind}, StudyId={studyId}, SiteId={siteId}.");
        }
        return kind;
    }
}