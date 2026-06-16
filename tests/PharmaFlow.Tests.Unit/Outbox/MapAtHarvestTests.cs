using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Sites;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Outbox;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Infrastructure.Persistence.Outbox;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Outbox;

// PFL-064 blind tests — map-at-harvest + integration-only outbox.
// Black-box against the ticket contract; they encode the NEW guarantees:
//   * only events with a domain→integration mapping produce an outbox row (integration-only);
//   * a skipped event still persists and still audits (ADR-0003 untouched);
//   * the row holds the serialized, versioned integration CONTRACT, not the raw domain event;
//   * the registry maps the contract name to the integration type, not the domain type;
//   * dispatch resolves the contract independently of the CLR domain type, so replay survives
//     a domain-shape change.
public class MapAtHarvestTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));

    // --- Integration-only: selectivity at harvest -------------------------------------

    [Fact]
    public async Task Only_the_mapped_event_writes_a_row_when_mapped_and_unmapped_save_togetherAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();

        ctx.Studies.Add(StudyBuilder.Create(Clock)); // StudyCreated -> mapped
        ctx.Sites.Add(NewSite());                    // SiteCreated  -> no mapping

        await ctx.SaveChangesAsync(ct);

        var message = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Equal("StudyCreated", message.Type);
    }

    [Fact]
    public async Task An_unmapped_event_writes_no_row_but_still_persists_and_auditsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();

        ctx.Sites.Add(NewSite()); // SiteCreated has no integration mapping

        await ctx.SaveChangesAsync(ct);

        // Integration-only: nothing written to the outbox for an unmapped event.
        Assert.Equal(0, await ctx.Set<OutboxMessage>().CountAsync(ct));
        // The aggregate itself still persists...
        Assert.Equal(1, await ctx.Sites.CountAsync(ct));
        // ...and the compliance log is untouched — ADR-0003's audit row still lands.
        Assert.True(await ctx.AuditEvents.AnyAsync(a => a.TargetEntityType == nameof(Site), ct));
    }

    // --- The row holds the versioned integration contract -----------------------------

    [Fact]
    public async Task Outbox_payload_is_the_integration_contract_with_a_version_stampAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var study = StudyBuilder.Create(Clock);

        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(ct);

        var message = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Equal("StudyCreated", message.Type); // stable contract name, not the CLR type name

        var contract = JsonSerializer.Deserialize<StudyCreatedIntegrationEvent>(
            message.Payload, OutboxSerialization.Options)!;
        Assert.Equal(study.Id.Value, contract.StudyId);
        Assert.Equal(Clock.UtcNow, contract.OccurredAt);
        Assert.Equal(1, contract.Version);

        // The domain StudyCreated has no Version field; its presence in the payload proves the
        // row stores the CONTRACT, not the raw domain event.
        Assert.Contains("version", message.Payload, StringComparison.OrdinalIgnoreCase);
    }

    // --- Registry flip: contract name -> integration type -----------------------------

    [Fact]
    public void Registry_maps_the_contract_name_to_the_integration_type_not_the_domain_type()
    {
        Assert.Equal(typeof(StudyCreatedIntegrationEvent), OutboxSerialization.Resolve("StudyCreated"));
        Assert.Equal("StudyCreated", OutboxSerialization.NameOf(
            new StudyCreatedIntegrationEvent(Guid.NewGuid(), Clock.UtcNow)));
    }

    // --- Replay decoupled from the domain type ----------------------------------------

    [Fact]
    public async Task Stored_contract_dispatches_independently_of_the_domain_type_so_replay_survives_a_domain_changeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = ProcessorContext();

        // Seed a row whose payload is the CONTRACT (versioned), not a domain event.
        var contract = new StudyCreatedIntegrationEvent(Guid.NewGuid(), Clock.UtcNow);
        var payload = JsonSerializer.Serialize(contract, contract.GetType(), OutboxSerialization.Options);
        ctx.Set<OutboxMessage>().Add(new OutboxMessage("StudyCreated", payload, Clock.UtcNow));
        await ctx.SaveChangesAsync(ct);

        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(ctx, dispatcher, Clock, new OutboxOptions());

        var count = await processor.ProcessBatchAsync(ct);

        // The processor resolved "StudyCreated" -> the integration type and dispatched the
        // contract without ever referencing the domain StudyCreated type.
        Assert.Equal(1, count);
        var dispatched = Assert.IsType<StudyCreatedIntegrationEvent>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(contract.StudyId, dispatched.StudyId);
        Assert.Equal(1, dispatched.Version); // version round-trips store -> dispatch

        var row = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.NotNull(row.ProcessedOn);
    }

    // ---------- helpers ----------

    private static Site NewSite()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.New(), "S-001", "Test Site", "US", UserId.New(), Clock);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"NewSite produced an invalid Site: {result.Error?.Message}");
        }

        return result.Value;
    }

    // Production-shaped context: both interceptors wired (audit + outbox harvest).
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"harvest-{Guid.NewGuid()}")
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, new SystemCurrentUser()))
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options);

    // Bare context for seeding outbox rows the processor will drain.
    private static AppDbContext ProcessorContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"harvest-proc-{Guid.NewGuid()}")
            .Options);

    private sealed class RecordingDispatcher : IIntegrationEventDispatcher
    {
        public List<INotification> Dispatched { get; } = [];

        public Task DispatchAsync(INotification notification, CancellationToken cancellationToken)
        {
            Dispatched.Add(notification);
            return Task.CompletedTask;
        }
    }
}