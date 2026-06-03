using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Sites.CreateSite;
using PharmaFlow.Application.Modules.Sites.CreateSite.Internal;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Modules.Sites;

// The seam test: CreateSiteHandler depends on the IStudiesModule *contract*, not on
// Studies' persistence. A hand-rolled fake (no NSubstitute) stands in for the module —
// `true` lets the create through, `false` short-circuits to study.not_found.
public class CreateSiteHandlerTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Existing_study_persists_site_and_returns_idAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var studies = new FakeStudiesModule(studyExists: true);
        var handler = new CreateSiteHandler(studies, ctx, Clock);

        var studyId = StudyId.New();
        var result = await handler.Handle(ValidCommand(studyId), ct);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotEqual(Guid.Empty, result.Value.Value);
        Assert.Equal(1, studies.ExistsCallCount);

        var site = await ctx.Sites.SingleAsync(ct);
        Assert.Equal(result.Value, site.Id);
        Assert.Equal(studyId, site.StudyId);
        Assert.Equal("SITE-001", site.SiteNumber);
    }

    [Fact]
    public async Task Unknown_study_returns_not_found_and_persists_nothingAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var studies = new FakeStudiesModule(studyExists: false);
        var handler = new CreateSiteHandler(studies, ctx, Clock);

        var result = await handler.Handle(ValidCommand(StudyId.New()), ct);

        Assert.True(result.IsFailure);
        Assert.Equal("study.not_found", result.Error.Code);
        Assert.Equal(1, studies.ExistsCallCount);
        Assert.Equal(0, await ctx.Sites.CountAsync(ct));
    }

    private static CreateSiteCommand ValidCommand(StudyId studyId) => new(
        StudyId: studyId,
        SiteNumber: "SITE-001",
        Name: "Massachusetts General Hospital",
        Country: "US",
        PrincipalInvestigatorUserId: UserId.New());

    // Mirrors IntegrationTestBase.CreateContext: the audit interceptor populates the
    // CreatedBy/UpdatedBy required columns on save, so it must be wired even in-memory.
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"create-site-{Guid.NewGuid()}")
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, new SystemCurrentUser()))
            .Options);

    // Hand-rolled stand-in for IStudiesModule (decision: no NSubstitute). Records the
    // existence-probe count so the false path proves the call path ran before short-circuit.
    private sealed class FakeStudiesModule(bool studyExists) : IStudiesModule
    {
        public int ExistsCallCount { get; private set; }

        public Task<bool> StudyExistsAsync(StudyId studyId, CancellationToken ct)
        {
            ExistsCallCount++;
            return Task.FromResult(studyExists);
        }

        public Task<StudyDto?> GetStudyByIdAsync(StudyId studyId, CancellationToken ct) =>
            Task.FromResult<StudyDto?>(null);
    }
}