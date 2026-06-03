using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Tests.Integration.Common.Helpers;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Modules.Sites;

// End-to-end exercise of the Sites pipeline AND the cross-module rule: HTTP POST →
// Mediator pipeline → CreateSiteHandler → IStudiesModule.StudyExistsAsync. A Study is
// seeded directly (its own module's persistence) so the Site create can confirm it
// through the contract — never an EF join.
public class CreateSitePipelineTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Valid_command_creates_siteAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var studyId = await SeedStudyAsync(ct);

        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(BuildPost(SiteBody(studyId.Value), "key-site-create"), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Location points at the per-resource URI /api/v1/sites/{id}.
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("/api/v1/sites/", location);
        var locationId = Guid.Parse(location["/api/v1/sites/".Length..]);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var responseId = doc.RootElement.GetProperty("value").GetGuid();
        Assert.Equal(locationId, responseId);

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(1, await verify.Sites.CountAsync(ct));
        Assert.True(await verify.Sites.AnyAsync(s => s.Id == new SiteId(responseId), ct));
    }

    [Fact]
    public async Task Unknown_study_returns_404_problem_detailsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        // No Study seeded → IStudiesModule.StudyExistsAsync returns false → handler short-circuits.
        using var response = await client.SendAsync(BuildPost(SiteBody(Guid.NewGuid()), "key-unknown"), ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("study.not_found", doc.RootElement.GetProperty("errorCode").GetString());

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(0, await verify.Sites.CountAsync(ct));
    }

    [Fact]
    public async Task Invalid_command_returns_400Async()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        // Empty SiteNumber → ValidationBehavior short-circuits before the handler (and before
        // the study-existence check), so no Study needs to exist.
        using var response = await client.SendAsync(BuildPost(SiteBody(Guid.NewGuid(), siteNumber: ""), "key-invalid"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _), "ProblemDetails must carry an 'errors' field.");

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(0, await verify.Sites.CountAsync(ct));
    }

    [Fact]
    public async Task Idempotent_replay_returns_cached_response_no_duplicateAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var studyId = await SeedStudyAsync(ct);

        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        const string key = "key-site-replay";
        var bodyObj = SiteBody(studyId.Value);

        using var first = await client.SendAsync(BuildPost(bodyObj, key), ct);
        var firstBody = await first.Content.ReadAsStringAsync(ct);

        using var second = await client.SendAsync(BuildPost(bodyObj, key), ct);
        var secondBody = await second.Content.ReadAsStringAsync(ct);

        Assert.Equal(first.StatusCode, second.StatusCode);
        Assert.Equal(firstBody, secondBody);

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(1, await verify.Sites.CountAsync(ct));
    }

    // ---------- helpers ----------

    // Seeds a Study via its own module's persistence (direct EF), returning its id so a
    // Site can reference it. This is the arrange step; the cross-module read is the act.
    private async Task<StudyId> SeedStudyAsync(CancellationToken ct)
    {
        var clock = new FrozenClock(DateTimeOffset.UtcNow);
        await using var seed = CreateContext(clock);

        var study = Study.Create(
            StudyId.New(),
            "PROTO-SITE",
            "Site Pipeline Study",
            StudyPhase.PhaseI,
            "Oncology",
            "Acme Pharma",
            100,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 9, 1),
            clock).Value;

        seed.Studies.Add(study);
        await seed.SaveChangesAsync(ct);
        return study.Id;
    }

    private static HttpRequestMessage BuildPost(object body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/sites")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static object SiteBody(Guid studyId, string siteNumber = "SITE-001") => new
    {
        studyId,
        siteNumber,
        name = "Massachusetts General Hospital",
        country = "US",
        principalInvestigatorUserId = UserId.System.Value,
    };
}