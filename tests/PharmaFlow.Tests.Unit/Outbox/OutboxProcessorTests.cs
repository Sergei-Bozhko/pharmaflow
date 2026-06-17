using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies.Events;
using PharmaFlow.Infrastructure.Outbox;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Outbox;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Outbox;

public class OutboxProcessorTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Successful_dispatch_marks_the_row_processedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        await SeedAsync(ctx, NewStudyCreatedRow(Clock.UtcNow), ct);
        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(ctx, dispatcher, Clock, new OutboxOptions());

        var count = await processor.ProcessBatchAsync(ct);

        Assert.Equal(1, count);
        Assert.Single(dispatcher.Dispatched);
        Assert.IsType<StudyCreatedIntegrationEvent>(dispatcher.Dispatched[0]);

        var row = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Equal(Clock.UtcNow, row.ProcessedOn);
        Assert.Equal(0, row.Attempts);
        Assert.Null(row.Error);
    }

    [Fact]
    public async Task Dispatch_failure_records_an_attempt_and_leaves_the_row_unprocessedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        await SeedAsync(ctx, NewStudyCreatedRow(Clock.UtcNow), ct);
        var processor = new OutboxProcessor(ctx, new ThrowingDispatcher(), Clock, new OutboxOptions());

        await processor.ProcessBatchAsync(ct);

        var row = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Null(row.ProcessedOn);
        Assert.Equal(1, row.Attempts);
        Assert.Contains("boom", row.Error);
    }

    [Fact]
    public async Task Already_processed_rows_are_not_re_dispatchedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var processed = NewStudyCreatedRow(Clock.UtcNow);
        processed.MarkProcessed(Clock.UtcNow);
        await SeedAsync(ctx, processed, ct);
        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(ctx, dispatcher, Clock, new OutboxOptions());

        var count = await processor.ProcessBatchAsync(ct);

        Assert.Equal(0, count);
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public async Task Rows_at_the_attempt_ceiling_are_parked_and_skippedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new OutboxOptions { MaxAttempts = 3 };
        await using var ctx = NewContext();
        var poison = NewStudyCreatedRow(Clock.UtcNow);
        for (var i = 0; i < options.MaxAttempts; i++)
        {
            poison.RecordFailure("previous failure");
        }
        await SeedAsync(ctx, poison, ct);
        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(ctx, dispatcher, Clock, options);

        var count = await processor.ProcessBatchAsync(ct);

        Assert.Equal(0, count);
        Assert.Empty(dispatcher.Dispatched);
        var row = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Null(row.ProcessedOn);
        Assert.Equal(options.MaxAttempts, row.Attempts); // untouched — still parked
    }

    [Fact]
    public async Task A_batch_processes_at_most_BatchSize_rowsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new OutboxOptions { BatchSize = 2 };
        await using var ctx = NewContext();
        await SeedAsync(ctx, ct,
            NewStudyCreatedRow(Clock.UtcNow),
            NewStudyCreatedRow(Clock.UtcNow.AddSeconds(1)),
            NewStudyCreatedRow(Clock.UtcNow.AddSeconds(2)));
        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(ctx, dispatcher, Clock, options);

        var count = await processor.ProcessBatchAsync(ct);

        Assert.Equal(2, count);
        Assert.Equal(2, dispatcher.Dispatched.Count);
        Assert.Equal(1, await ctx.Set<OutboxMessage>().CountAsync(m => m.ProcessedOn == null, ct));
    }

    [Fact]
    public async Task Rows_are_processed_oldest_firstAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new OutboxOptions { BatchSize = 1 };
        await using var ctx = NewContext();
        var older = NewStudyCreatedRow(Clock.UtcNow);
        var newer = NewStudyCreatedRow(Clock.UtcNow.AddMinutes(5));
        await SeedAsync(ctx, ct, newer, older); // insert newest first to prove ordering, not insertion order
        var processor = new OutboxProcessor(ctx, new RecordingDispatcher(), Clock, options);

        await processor.ProcessBatchAsync(ct);

        var olderRow = await ctx.Set<OutboxMessage>().SingleAsync(m => m.Id == older.Id, ct);
        var newerRow = await ctx.Set<OutboxMessage>().SingleAsync(m => m.Id == newer.Id, ct);
        Assert.NotNull(olderRow.ProcessedOn); // oldest was taken first
        Assert.Null(newerRow.ProcessedOn);
    }

    // PFL-065: the processor threads the outbox row id to the dispatcher as the dedup key the
    // consumer inbox (PFL-066) needs — transport-agnostic, proven here without HTTP.
    [Fact]
    public async Task Dispatch_receives_the_outbox_row_id_as_the_dedup_keyAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var row = NewStudyCreatedRow(Clock.UtcNow);
        await SeedAsync(ctx, row, ct);
        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(ctx, dispatcher, Clock, new OutboxOptions());

        await processor.ProcessBatchAsync(ct);

        Assert.Equal(row.Id, Assert.Single(dispatcher.DispatchedIds));
    }

    [Fact]
    public async Task Unregistered_event_type_is_treated_as_a_failure_not_a_crashAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        await SeedAsync(ctx, new OutboxMessage("NotARealEvent", "{}", Clock.UtcNow), ct);
        var processor = new OutboxProcessor(ctx, new RecordingDispatcher(), Clock, new OutboxOptions());

        await processor.ProcessBatchAsync(ct); // must not throw out of the batch

        var row = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Null(row.ProcessedOn);
        Assert.Equal(1, row.Attempts);
        Assert.Contains("NotARealEvent", row.Error);
    }

    private static OutboxMessage NewStudyCreatedRow(DateTimeOffset occurredOn)
    {
        var @event = new StudyCreated(StudyId.New(), occurredOn);
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), OutboxSerialization.Options);
        return new OutboxMessage("StudyCreated", payload, occurredOn);
    }

    private static async Task SeedAsync(AppDbContext ctx, OutboxMessage row, CancellationToken ct) =>
        await SeedAsync(ctx, ct, row);

    private static async Task SeedAsync(AppDbContext ctx, CancellationToken ct, params OutboxMessage[] rows)
    {
        ctx.Set<OutboxMessage>().AddRange(rows);
        await ctx.SaveChangesAsync(ct);
    }

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"outbox-proc-{Guid.NewGuid()}")
            .Options);

    private sealed class RecordingDispatcher : IIntegrationEventDispatcher
    {
        public List<INotification> Dispatched { get; } = [];
        public List<Guid> DispatchedIds { get; } = [];

        public Task DispatchAsync(INotification notification, Guid messageId, CancellationToken cancellationToken)
        {
            Dispatched.Add(notification);
            DispatchedIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDispatcher : IIntegrationEventDispatcher
    {
        public Task DispatchAsync(INotification notification, Guid messageId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }
}