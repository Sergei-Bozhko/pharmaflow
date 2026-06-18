using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Outbox;
using PharmaFlow.Infrastructure.Persistence.Outbox;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Consumer;

// PFL-066 — the receiving half + the rollback drill, against real Postgres + the in-memory
// HTTP boundary. Black-box on the ticket AC:
//   * the webhook translates the wire event through the ACL into KnownStudy and records it in the inbox;
//   * a repeated message id is deduped by the inbox (effectively-once across the boundary);
//   * driven over HTTP (flag=Http) the full producer→outbox→POST→ACL chain projects the study;
//   * a forced redelivery produces no duplicate row;
//   * the rollback drill — flip Http→InProc mid-stream — loses and duplicates nothing.
public class IntegrationEventWebhookTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string WebhookPath = "internal/integration-events/study-created";

    private static readonly DateTimeOffset Occurred =
        new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    // --- 1. Webhook → ACL → KnownStudy, recorded in the inbox -------------------------
    [Fact]
    public async Task Webhook_translates_through_the_acl_and_projects_a_known_studyAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();
        var messageId = Guid.NewGuid();
        var studyId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(WebhookPath, Envelope(messageId, studyId), ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var verify = CreateContext(new FrozenClock(Occurred));
        var known = await verify.Set<KnownStudy>().SingleAsync(ct);
        Assert.Equal(studyId, known.StudyId);
        var inbox = await verify.Set<InboxMessage>().SingleAsync(ct);
        Assert.Equal(messageId, inbox.MessageId); // the dedup key the consumer keeps for itself
    }

    // --- 2. Inbox dedup: the same message id twice yields one row ----------------------
    [Fact]
    public async Task A_repeated_message_id_is_deduped_by_the_inboxAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();
        var messageId = Guid.NewGuid();
        var studyId = Guid.NewGuid();

        using (var first = await client.PostAsJsonAsync(WebhookPath, Envelope(messageId, studyId), ct))
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using (var second = await client.PostAsJsonAsync(WebhookPath, Envelope(messageId, studyId), ct))
            Assert.Equal(HttpStatusCode.OK, second.StatusCode); // a seen id is a no-op, still 2xx

        await using var verify = CreateContext(new FrozenClock(Occurred));
        Assert.Equal(1, await verify.Set<KnownStudy>().CountAsync(ct));
        Assert.Equal(1, await verify.Set<InboxMessage>().CountAsync(ct));
    }

    // --- 3. End-to-end over HTTP (flag = Http) ----------------------------------------
    [Fact]
    public async Task Event_is_delivered_over_http_and_projected_when_the_flag_is_setAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebhookWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();
        SetTransport(factory, OutboxOptions.IntegrationTransport.Http);

        var studyId = await CreateStudyAsync(client, "key-http-e2e", ct);
        Assert.Equal(1, await DrainAsync(factory, ct)); // producer POSTs to the webhook over HTTP

        await using var verify = CreateContext(new FrozenClock(Occurred));
        var known = await verify.Set<KnownStudy>().SingleAsync(ct);
        Assert.Equal(studyId, known.StudyId);
        var message = await verify.Set<OutboxMessage>().SingleAsync(ct);
        Assert.NotNull(message.ProcessedOn);                          // a 2xx marked the row done
        Assert.Equal(1, await verify.Set<InboxMessage>().CountAsync(ct)); // consumer recorded it once
    }

    // --- 4. Redelivery over HTTP produces no duplicate --------------------------------
    [Fact]
    public async Task Redelivery_over_http_does_not_duplicate_the_projectionAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebhookWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();
        SetTransport(factory, OutboxOptions.IntegrationTransport.Http);

        await CreateStudyAsync(client, "key-http-replay", ct);
        Assert.Equal(1, await DrainAsync(factory, ct));

        // Simulate a crash between the consumer's success and the producer's processed-write:
        // null processed_on so the producer re-POSTs the same message id.
        await using (var arrange = CreateContext(new FrozenClock(Occurred)))
        {
            await arrange.Database.ExecuteSqlRawAsync("UPDATE outbox_messages SET processed_on = NULL", ct);
        }
        Assert.Equal(1, await DrainAsync(factory, ct)); // re-delivered; the inbox absorbs it

        await using var verify = CreateContext(new FrozenClock(Occurred));
        Assert.Equal(1, await verify.Set<KnownStudy>().CountAsync(ct));   // effectively-once
        Assert.Equal(1, await verify.Set<InboxMessage>().CountAsync(ct));
    }

    // --- 5. Rollback drill: flip Http → InProc with no loss or duplication -------------
    [Fact]
    public async Task Rollback_drill_flips_transport_with_no_loss_or_duplicationAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebhookWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        // On HTTP: deliver the first study over the wire.
        SetTransport(factory, OutboxOptions.IntegrationTransport.Http);
        var overHttp = await CreateStudyAsync(client, "key-drill-http", ct, "PROTO-DRILL-1");
        Assert.Equal(1, await DrainAsync(factory, ct));

        // Pull the lever back to in-proc — a config flip, no redeploy — and deliver the second.
        SetTransport(factory, OutboxOptions.IntegrationTransport.InProc);
        var overInProc = await CreateStudyAsync(client, "key-drill-inproc", ct, "PROTO-DRILL-2");
        Assert.Equal(1, await DrainAsync(factory, ct));

        // Both studies projected exactly once across the flip; nothing lost, nothing duplicated.
        await using var verify = CreateContext(new FrozenClock(Occurred));
        var studyIds = await verify.Set<KnownStudy>().Select(k => k.StudyId).ToListAsync(ct);
        Assert.Equal(2, studyIds.Count);
        Assert.Contains(overHttp, studyIds);
        Assert.Contains(overInProc, studyIds);
        Assert.Equal(0, await verify.Set<OutboxMessage>().CountAsync(m => m.ProcessedOn == null, ct));
    }

    // ---------- helpers ----------

    private static object Envelope(Guid messageId, Guid studyId) => new
    {
        messageId,
        type = "StudyCreated",
        payload = JsonSerializer.Serialize(
            new StudyCreatedTransportDto(studyId, Occurred, 1), OutboxSerialization.Options),
    };

    private static void SetTransport(WebApplicationFactory<Program> factory, OutboxOptions.IntegrationTransport transport) =>
        factory.Services.GetRequiredService<OutboxOptions>().Transport = transport;

    private static async Task<int> DrainAsync(WebApplicationFactory<Program> factory, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(ct);
    }

    private static async Task<Guid> CreateStudyAsync(
        HttpClient client, string idempotencyKey, CancellationToken ct, string protocolNumber = "PROTO-001")
    {
        using var response = await client.SendAsync(BuildPost(ValidBody(protocolNumber), idempotencyKey), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("value").GetGuid();
    }

    private static HttpRequestMessage BuildPost(object body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/studies")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static object ValidBody(string protocolNumber = "PROTO-001") => new
    {
        protocolNumber,
        title = "Phase I Oncology Study",
        phase = (int)StudyPhase.PhaseI,
        therapeuticArea = "Oncology",
        sponsorOrganization = "Acme Pharma",
        plannedEnrolment = 100,
        plannedStartDate = "2026-06-01",
        plannedEndDate = "2026-09-01",
    };
}