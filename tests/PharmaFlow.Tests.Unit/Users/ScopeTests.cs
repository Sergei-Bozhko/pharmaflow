using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Users;

namespace PharmaFlow.Tests.Unit.Users;

public class ScopeTests
{
    [Fact]
    public void System_factory_returns_kind_System_with_both_ids_null()
    {
        var scope = Scope.System();

        Assert.Equal(ScopeKind.System, scope.Kind);
        Assert.Null(scope.StudyId);
        Assert.Null(scope.SiteId);
    }

    [Fact]
    public void ForStudy_factory_returns_kind_Study_with_StudyId_set_and_SiteId_null()
    {
        var studyId = StudyId.New();

        var scope = Scope.ForStudy(studyId);

        Assert.Equal(ScopeKind.Study, scope.Kind);
        Assert.Equal(studyId, scope.StudyId);
        Assert.Null(scope.SiteId);
    }

    [Fact]
    public void ForSite_factory_returns_kind_Site_with_SiteId_set_and_StudyId_null()
    {
        var siteId = SiteId.New();

        var scope = Scope.ForSite(siteId);

        Assert.Equal(ScopeKind.Site, scope.Kind);
        Assert.Equal(siteId, scope.SiteId);
        Assert.Null(scope.StudyId);
    }

    [Fact]
    public void Constructing_System_kind_with_StudyId_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Scope(ScopeKind.System, StudyId.New(), null));
    }

    [Fact]
    public void Constructing_Study_kind_with_null_StudyId_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Scope(ScopeKind.Study, null, null));
    }

    [Fact]
    public void Constructing_Site_kind_with_null_SiteId_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Scope(ScopeKind.Site, null, null));
    }
}