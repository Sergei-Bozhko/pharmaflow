using System.Net;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Infrastructure.Outbox;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Outbox;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Outbox;

// PFL-065 HTTP transport — black-box against a stubbed HttpMessageHandler, no live socket. Encodes:
//   * the POST carries the dedup key (messageId) + the serialized contract the consumer inbox needs;
//   * a non-2xx response throws, so the processor's catch records a failed attempt (at-least-once);
//   * driven through a real OutboxProcessor, a 500 leaves the row unprocessed with attempts++.
public class HttpIntegrationEventDispatcherTests
{
    private static readonly DateTimeOffset Occurred =
        new(2026, 6, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Posts_the_message_id_and_contract_to_the_consumer_webhookAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new StubHandler(HttpStatusCode.OK);
        var dispatcher = new HttpIntegrationEventDispatcher(NewClient(handler));
        var messageId = Guid.NewGuid();
        var studyId = Guid.NewGuid();

        await dispatcher.DispatchAsync(
            new StudyCreatedIntegrationEvent(studyId, Occurred), messageId, ct);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Contains("integration-events", handler.Request.RequestUri!.ToString(), StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(handler.Body!);
        var root = doc.RootElement;
        Assert.Equal(messageId, root.GetProperty("messageId").GetGuid()); // the dedup key
        Assert.Equal("StudyCreated", root.GetProperty("type").GetString());

        // The payload is the serialized contract; the consumer rehydrates it via the same registry.
        var payload = root.GetProperty("payload").GetString()!;
        var contract = JsonSerializer.Deserialize<StudyCreatedIntegrationEvent>(
            payload, OutboxSerialization.Options)!;
        Assert.Equal(studyId, contract.StudyId);
        Assert.Equal(1, contract.Version);
    }

    [Fact]
    public async Task Non_2xx_response_throws_so_the_processor_can_record_a_failed_attemptAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var dispatcher = new HttpIntegrationEventDispatcher(NewClient(new StubHandler(HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
            dispatcher.DispatchAsync(new StudyCreatedIntegrationEvent(Guid.NewGuid(), Occurred), Guid.NewGuid(), ct));
    }

    [Fact]
    public async Task A_500_from_the_consumer_leaves_the_row_unprocessed_with_an_incremented_attemptAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"http-fail-{Guid.NewGuid()}").Options);

        var contract = new StudyCreatedIntegrationEvent(Guid.NewGuid(), Occurred);
        var payload = JsonSerializer.Serialize(contract, contract.GetType(), OutboxSerialization.Options);
        ctx.Set<OutboxMessage>().Add(new OutboxMessage("StudyCreated", payload, Occurred));
        await ctx.SaveChangesAsync(ct);

        var dispatcher = new HttpIntegrationEventDispatcher(NewClient(new StubHandler(HttpStatusCode.InternalServerError)));
        var processor = new OutboxProcessor(ctx, dispatcher, new FrozenClock(Occurred), new OutboxOptions());

        await processor.ProcessBatchAsync(ct); // the throw is caught and turned into a failed attempt

        var row = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Null(row.ProcessedOn); // at-least-once: a failed POST never marks the row done
        Assert.Equal(1, row.Attempts);
        Assert.NotNull(row.Error);
    }

    private static HttpClient NewClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://localhost") };

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }
}