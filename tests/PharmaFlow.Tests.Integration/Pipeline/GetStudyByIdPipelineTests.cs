using System.Globalization;
using System.Net;
using System.Text.Json;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Pipeline;

// GetStudyById query path: HTTP GET → Mediator → Logging → Validation →
// (Idempotency/Transaction bypassed — query has no command marker) → handler.
// Arranges rows directly via CreateContext, then exercises the real HTTP path.
public class GetStudyByIdPipelineTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset FrozenInstant =
        DateTimeOffset.Parse("2026-05-31T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Existing_study_returns_200_with_dtoAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        var study = await PersistStudyAsync(ct);

        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"api/v1/studies/{study.Id.Value}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal(study.Id.Value, root.GetProperty("id").GetGuid());
        Assert.Equal("PROTO-051", root.GetProperty("protocolNumber").GetString());
        Assert.Equal("Get Study Test", root.GetProperty("title").GetString());
        Assert.Equal((int)StudyPhase.PhaseII, root.GetProperty("phase").GetInt32());
        Assert.Equal(50, root.GetProperty("plannedEnrolment").GetInt32());
        Assert.Equal((int)StudyStatus.Draft, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Nonexistent_study_returns_404_problem_detailsAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"api/v1/studies/{Guid.NewGuid()}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("study.not_found", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Soft_deleted_study_returns_404Async()
    {
        var ct = TestContext.Current.CancellationToken;

        var study = await PersistStudyAsync(ct);

        // Soft-delete the row; the global query filter (PFL-028) must hide it from the query.
        await using (var ctx = CreateContext(new FrozenClock(FrozenInstant)))
        {
            ctx.Studies.Attach(study);
            ctx.Entry(study).Property("IsDeleted").CurrentValue = true;
            await ctx.SaveChangesAsync(ct);
        }

        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"api/v1/studies/{study.Id.Value}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- helpers ----------

    private async Task<Study> PersistStudyAsync(CancellationToken ct)
    {
        var clock = new FrozenClock(FrozenInstant);
        var result = Study.Create(
            StudyId.New(),
            "PROTO-051",
            "Get Study Test",
            StudyPhase.PhaseII,
            "Cardiology",
            "Acme Pharma Inc",
            50,
            DateOnly.FromDateTime(FrozenInstant.UtcDateTime),
            DateOnly.FromDateTime(FrozenInstant.AddDays(180).UtcDateTime),
            clock);

        Assert.True(result.IsSuccess, result.Error?.Message);

        await using var ctx = CreateContext(clock);
        ctx.Studies.Add(result.Value);
        await ctx.SaveChangesAsync(ct);

        return result.Value;
    }
}