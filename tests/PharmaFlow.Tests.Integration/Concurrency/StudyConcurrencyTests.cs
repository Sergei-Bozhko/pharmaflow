using System.Globalization;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Tests.Integration.Common.Helpers;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Concurrency;

public class StudyConcurrencyTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset FrozenInstant =
        DateTimeOffset.Parse("2026-05-10T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Parallel_updates_surface_typed_concurrency_errorAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new FrozenClock(FrozenInstant);
        var studyId = await ArrangeActiveStudyAsync(clock, ct);

        await using var ctxA = CreateContext(clock);
        await using var ctxB = CreateContext(clock);

        var studyA = await ctxA.Studies.SingleAsync(s => s.Id == studyId, ct);
        var studyB = await ctxB.Studies.SingleAsync(s => s.Id == studyId, ct);

        var sig = new SignatureMeta(SignatureId.New(), UserId.System, FrozenInstant, "Suspension");

        var taskA = Task.Run(async () =>
        {
            var transition = studyA.Suspend(sig, "reason A", clock);
            Assert.True(transition.IsSuccess, transition.Error?.Message);
            return await ctxA.SaveChangesWithConcurrencyMappingAsync(ct);
        }, ct);

        var taskB = Task.Run(async () =>
        {
            var transition = studyB.Suspend(sig, "reason B", clock);
            Assert.True(transition.IsSuccess, transition.Error?.Message);
            return await ctxB.SaveChangesWithConcurrencyMappingAsync(ct);
        }, ct);

        var results = await Task.WhenAll(taskA, taskB);

        Assert.Single(results, r => r.IsSuccess);
        var failure = Assert.Single(results, r => r.IsFailure);
        Assert.Equal("concurrency", failure.Error.Code);
        Assert.Equal(ErrorType.Conflict, failure.Error.ErrorType);

        await using var assertCtx = CreateContext(clock);
        var winning = await assertCtx.Studies.SingleAsync(s => s.Id == studyId, ct);
        Assert.Equal(StudyStatus.Suspended, winning.Status);
    }

    [Fact]
    public async Task Sequential_updates_succeed_when_no_concurrent_modificationAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new FrozenClock(FrozenInstant);
        var studyId = await ArrangeActiveStudyAsync(clock, ct);

        var sig = new SignatureMeta(SignatureId.New(), UserId.System, FrozenInstant, "Lifecycle");

        await using (var ctx = CreateContext(clock))
        {
            var study = await ctx.Studies.SingleAsync(s => s.Id == studyId, ct);
            Assert.True(study.Suspend(sig, "reason", clock).IsSuccess);
            var saved = await ctx.SaveChangesWithConcurrencyMappingAsync(ct);
            Assert.True(saved.IsSuccess);
        }

        await using (var ctx = CreateContext(clock))
        {
            var study = await ctx.Studies.SingleAsync(s => s.Id == studyId, ct);
            Assert.True(study.Close(sig, "wrap-up", clock).IsSuccess);
            var saved = await ctx.SaveChangesWithConcurrencyMappingAsync(ct);
            Assert.True(saved.IsSuccess);
        }

        await using var assertCtx = CreateContext(clock);
        var final = await assertCtx.Studies.SingleAsync(s => s.Id == studyId, ct);
        Assert.Equal(StudyStatus.Closed, final.Status);
    }

    private async Task<StudyId> ArrangeActiveStudyAsync(FrozenClock clock, CancellationToken ct)
    {
        var study = BuildStudy(clock);

        await using var ctx = CreateContext(clock);
        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(ct);

        Assert.True(study.SubmitForApproval().IsSuccess);
        await ctx.SaveChangesAsync(ct);

        var sig = new SignatureMeta(SignatureId.New(), UserId.System, FrozenInstant, "Activation");
        Assert.True(study.Activate(sig, clock).IsSuccess);
        await ctx.SaveChangesAsync(ct);

        return study.Id;
    }

    private static Study BuildStudy(FrozenClock clock)
    {
        var plannedStart = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var plannedEnd = DateOnly.FromDateTime(clock.UtcNow.AddDays(90).UtcDateTime);

        var result = Study.Create(
            StudyId.New(),
            "TestProtocol",
            "testTitle",
            StudyPhase.PhaseI,
            "OncologyTest",
            "TestSponsor",
            100,
            plannedStart,
            plannedEnd,
            clock);

        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }
}