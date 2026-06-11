using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Outbox;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Infrastructure.Persistence.Outbox;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Outbox;

// PFL-062: the two claims the sprint stands on, against real Postgres transaction semantics
// (InMemory can't show rollback). Black-box — these encode the contract; an impl that writes
// the outbox row in a separate transaction fails the atomicity test.
public class OutboxEndToEndTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // --- 1. Happy path: command captures aggregate + outbox row, processor projects the read-model.
    [Fact]
    public async Task Command_writes_aggregate_and_outbox_row_then_processor_projects_known_studyAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(BuildPost(ValidBody(), "key-e2e"), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var studyId = doc.RootElement.GetProperty("value").GetGuid();

        // Aggregate + exactly one unprocessed outbox row, written atomically by the command.
        await using (var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            Assert.Equal(1, await verify.Studies.CountAsync(ct));
            var message = await verify.Set<OutboxMessage>().SingleAsync(ct);
            Assert.Equal("StudyCreated", message.Type);
            Assert.Null(message.ProcessedOn);
            Assert.Equal(0, await verify.Set<KnownStudy>().CountAsync(ct)); // not dispatched yet
        }

        // Drive the processor deterministically (hosted timer removed in the test host).
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
            Assert.Equal(1, await processor.ProcessBatchAsync(ct));
        }

        // Dispatched: row marked processed, and Sites projected the known study (PFL-061).
        await using (var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            var message = await verify.Set<OutboxMessage>().SingleAsync(ct);
            Assert.NotNull(message.ProcessedOn);
            var known = await verify.Set<KnownStudy>().SingleAsync(ct);
            Assert.Equal(studyId, known.StudyId);
        }
    }

    // --- 2. Atomicity: a failure within the transaction rolls back aggregate AND outbox row.
    //     The non-negotiable test — proves the outbox row shares the aggregate's transaction.
    [Fact]
    public async Task Aggregate_and_outbox_row_roll_back_together_on_failure_within_the_transactionAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new FrozenClock(DateTimeOffset.UtcNow);

        await using (var ctx = CreateOutboxContext(clock))
        await using (var tx = await ctx.Database.BeginTransactionAsync(ct))
        {
            ctx.Studies.Add(StudyBuilder.Create(clock));

            // The outbox interceptor harvests StudyCreated into THIS SaveChanges, so the study
            // and its outbox row are written together inside the open transaction.
            await ctx.SaveChangesAsync(ct);

            try
            {
                // Force a failure after the event was raised but before commit.
                await ctx.Database.ExecuteSqlRawAsync("SELECT 1/0", ct);
            }
            catch (PostgresException)
            {
                await tx.RollbackAsync(ct);
            }
        }

        // Neither survives — one transaction, both or neither.
        await using var verify = CreateContext(clock);
        Assert.Equal(0, await verify.Studies.CountAsync(ct));
        Assert.Equal(0, await verify.Set<OutboxMessage>().CountAsync(ct));
    }

    // --- 3. At-least-once replay: a re-delivered message produces no duplicate side effect.
    //     Belt-and-braces — the idempotent subscriber absorbs the replay.
    [Fact]
    public async Task Redelivered_message_does_not_duplicate_the_projectionAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new PharmaFlowWebApplicationFactory(Fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(BuildPost(ValidBody(), "key-replay-e2e"), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // First delivery: projects exactly one KnownStudy row.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(ct));
        }
        await using (var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            Assert.Equal(1, await verify.Set<KnownStudy>().CountAsync(ct));
        }

        // Simulate a crash between subscriber success and the processed-write: clear ProcessedOn
        // so the processor re-delivers the same message.
        await using (var arrange = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            await arrange.Database.ExecuteSqlRawAsync("UPDATE outbox_messages SET processed_on = NULL", ct);
        }

        // Re-deliver: processor dispatches again; the idempotent subscriber no-ops on the existing row.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(ct));
        }

        await using (var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            Assert.Equal(1, await verify.Set<KnownStudy>().CountAsync(ct)); // no duplicate side effect
            var message = await verify.Set<OutboxMessage>().SingleAsync(ct);
            Assert.NotNull(message.ProcessedOn); // re-processed
        }
    }

    // --- 4. Poison/retry (lighter): a failing message records an attempt and parks at the ceiling
    //     without wedging the loop or vanishing.
    [Fact]
    public async Task Poison_message_records_failure_then_parks_at_the_attempt_ceilingAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Unregistered Type → deserialization throws before dispatch (a deterministic poison row).
        await using (var arrange = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            arrange.Set<OutboxMessage>().Add(new OutboxMessage("UnregisteredEvent", "{}", DateTimeOffset.UtcNow));
            await arrange.SaveChangesAsync(ct);
        }

        var options = new OutboxOptions { MaxAttempts = 1 };

        // First pass: failure recorded, row left unprocessed — the batch still completes.
        await using (var ctx = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            var processor = new OutboxProcessor(ctx, new NoopDispatcher(), new FrozenClock(DateTimeOffset.UtcNow), options);
            Assert.Equal(1, await processor.ProcessBatchAsync(ct));
        }
        await using (var verify = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            var message = await verify.Set<OutboxMessage>().SingleAsync(ct);
            Assert.Equal(1, message.Attempts);
            Assert.Null(message.ProcessedOn);
            Assert.NotNull(message.Error);
        }

        // Second pass: at the ceiling → parked, not retried forever.
        await using (var ctx = CreateContext(new FrozenClock(DateTimeOffset.UtcNow)))
        {
            var processor = new OutboxProcessor(ctx, new NoopDispatcher(), new FrozenClock(DateTimeOffset.UtcNow), options);
            Assert.Equal(0, await processor.ProcessBatchAsync(ct));
        }
    }

    // ---------- helpers ----------

    // A context wired like production (outbox interceptor harvesting domain events), unlike the
    // base CreateContext which carries only the auditing interceptor.
    private AppDbContext CreateOutboxContext(IClock clock)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new AuditingSaveChangesInterceptor(clock, new SystemCurrentUser()),
                new OutboxSaveChangesInterceptor())
            .Options;

        return new AppDbContext(options);
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

    private sealed class NoopDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}