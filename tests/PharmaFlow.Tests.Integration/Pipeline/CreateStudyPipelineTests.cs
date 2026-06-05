using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Pipeline;

// First end-to-end exercise of the full pipeline: HTTP POST → Mediator →
// Logging → Validation → Idempotency → Audit → Transaction → CreateStudyHandler.
// Shares the Testcontainers Postgres with the verification DbContext via the
// fixture connection string, so HTTP writes are visible to direct EF queries.
public class CreateStudyPipelineTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Valid_command_creates_study_and_writes_audit_rowsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(BuildPost(ValidBody(), "key-create"), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Location points at the per-resource URI /api/v1/studies/{id}.
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("/api/v1/studies/", location);
        var locationId = Guid.Parse(location["/api/v1/studies/".Length..]);

        // Response body carries the new StudyId (record struct → { "value": "<guid>" }).
        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var responseId = doc.RootElement.GetProperty("value").GetGuid();
        Assert.Equal(locationId, responseId);

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));

        // studies row persisted.
        Assert.Equal(1, await verify.Studies.CountAsync(ct));
        Assert.True(await verify.Studies.AnyAsync(s => s.Id == new Domain.Common.Ids.StudyId(responseId), ct));

        // TWO audit rows: row-level Create (PFL-030 interceptor) + command-level CommandOutcome (PFL-046).
        var audits = await verify.AuditEvents.ToListAsync(ct);
        Assert.Equal(2, audits.Count);
        Assert.Single(audits, a => a.EventType == AuditEventType.Create && a.TargetEntityType == "Study");
        Assert.Single(audits, a => a.EventType == AuditEventType.CommandOutcome);
    }

    [Fact]
    public async Task Invalid_command_returns_400_problem_detailsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        // Missing protocol number → ValidationBehavior short-circuits before the handler.
        using var response = await client.SendAsync(
            BuildPost(ValidBody(protocolNumber: ""), "key-invalid"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _), "ProblemDetails must carry an 'errors' field.");

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(0, await verify.Studies.CountAsync(ct));
    }

    [Fact]
    public async Task Idempotent_replay_returns_cached_response_no_duplicateAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        const string key = "key-replay";

        using var first = await client.SendAsync(BuildPost(ValidBody(), key), ct);
        var firstBody = await first.Content.ReadAsStringAsync(ct);

        using var second = await client.SendAsync(BuildPost(ValidBody(), key), ct);
        var secondBody = await second.Content.ReadAsStringAsync(ct);

        Assert.Equal(first.StatusCode, second.StatusCode);
        Assert.Equal(firstBody, secondBody);

        // Cached replay must not insert a second study.
        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(1, await verify.Studies.CountAsync(ct));
    }

    [Fact]
    public async Task Idempotent_replay_different_body_returns_409Async()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        const string key = "key-mismatch";

        using var first = await client.SendAsync(BuildPost(ValidBody(title: "First Title"), key), ct);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Same key, different body → IdempotencyBehavior returns Conflict.
        using var second = await client.SendAsync(BuildPost(ValidBody(title: "Different Title"), key), ct);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var body = await second.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("idempotency.body_mismatch", doc.RootElement.GetProperty("errorCode").GetString());

        await using var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow));
        Assert.Equal(1, await verify.Studies.CountAsync(ct));
    }

    // ---------- helpers ----------

    private static HttpRequestMessage BuildPost(object body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/studies")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static object ValidBody(string protocolNumber = "PROTO-001", string title = "Phase I Oncology Study") => new
    {
        protocolNumber,
        title,
        phase = (int)StudyPhase.PhaseI,
        therapeuticArea = "Oncology",
        sponsorOrganization = "Acme Pharma",
        plannedEnrolment = 100,
        plannedStartDate = "2026-06-01",
        plannedEndDate = "2026-09-01",
    };
}