using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Tests.Unit.Modules.Sites;

// PFL-061 subscriber. The cross-module reaction: Sites projects a local read-model of
// known studies, fed by the integration event. The contract under test is idempotency —
// at-least-once delivery may re-present the same event, and the end state must not change.
public class StudyCreatedHandlerTests
{
    private static readonly DateTimeOffset Occurred =
        new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handling_the_same_event_twice_creates_one_rowAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var handler = new StudyCreatedHandler(ctx);
        var @event = new StudyCreatedIntegrationEvent(Guid.NewGuid(), Occurred);

        await handler.Handle(@event, ct);
        await handler.Handle(@event, ct);

        Assert.Equal(1, await ctx.Set<KnownStudy>().CountAsync(ct));
    }

    [Fact]
    public async Task Distinct_studies_each_get_their_own_rowAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var handler = new StudyCreatedHandler(ctx);

        await handler.Handle(new StudyCreatedIntegrationEvent(Guid.NewGuid(), Occurred), ct);
        await handler.Handle(new StudyCreatedIntegrationEvent(Guid.NewGuid(), Occurred), ct);

        Assert.Equal(2, await ctx.Set<KnownStudy>().CountAsync(ct));
    }

    [Fact]
    public async Task The_projected_row_carries_the_event_id_and_timestampAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = NewContext();
        var handler = new StudyCreatedHandler(ctx);
        var studyId = Guid.NewGuid();

        await handler.Handle(new StudyCreatedIntegrationEvent(studyId, Occurred), ct);

        var row = await ctx.Set<KnownStudy>().SingleAsync(ct);
        Assert.Equal(studyId, row.StudyId);
        Assert.Equal(Occurred, row.RegistredAt);
    }

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"known-study-{Guid.NewGuid()}")
            .Options);
}