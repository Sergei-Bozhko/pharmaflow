using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Studies.Events;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Infrastructure.Persistence.Outbox;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Outbox;

public class OutboxInterceptorTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 6, 9, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Saving_an_aggregate_that_raised_an_event_writes_one_matching_outbox_rowAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var study = StudyBuilder.Create(Clock); // Study.Create raises StudyCreated

        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(ct);

        var message = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        Assert.Equal("StudyCreated", message.Type);
        Assert.Equal(Clock.UtcNow, message.OccurredOn);
        Assert.Null(message.ProcessedOn);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.Error);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public async Task Payload_serializes_the_id_as_a_bare_guid_not_a_wrapper_objectAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var study = StudyBuilder.Create(Clock);

        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(ct);

        var message = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        // The strongly-typed-id JSON converter must flatten StudyId to its inner guid string.
        Assert.Contains(study.Id.Value.ToString(), message.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"value\"", message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payload_round_trips_back_to_the_event_via_the_registryAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var study = StudyBuilder.Create(Clock);

        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(ct);

        var message = await ctx.Set<OutboxMessage>().SingleAsync(ct);
        var clrType = OutboxSerialization.Resolve(message.Type);
        var restored = (StudyCreatedIntegrationEvent)JsonSerializer.Deserialize(
            message.Payload, clrType, OutboxSerialization.Options)!;

        Assert.Equal(study.Id.Value, restored.StudyId);
        Assert.Equal(Clock.UtcNow, restored.OccurredAt);
    }

    [Fact]
    public async Task A_second_save_does_not_re_harvest_the_same_eventAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var study = StudyBuilder.Create(Clock);

        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(ct);
        await ctx.SaveChangesAsync(ct); // events already dequeued off the aggregate

        Assert.Equal(1, await ctx.Set<OutboxMessage>().CountAsync(ct));
    }

    [Fact]
    public async Task Each_aggregate_in_a_save_produces_its_own_rowAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();

        ctx.Studies.Add(StudyBuilder.Create(Clock));
        ctx.Studies.Add(StudyBuilder.Create(Clock));
        await ctx.SaveChangesAsync(ct);

        var messages = await ctx.Set<OutboxMessage>().ToListAsync(ct);
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal("StudyCreated", m.Type));
    }

    // Mirrors CreateStudyHandlerTests.NewContext: the audit interceptor populates the
    // required CreatedBy/UpdatedBy columns on save, so it must be wired even in-memory;
    // the outbox interceptor under test is added alongside it.
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"outbox-{Guid.NewGuid()}")
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, new SystemCurrentUser()))
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options);
}