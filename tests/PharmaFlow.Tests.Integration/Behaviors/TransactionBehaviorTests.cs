using System.Globalization;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Behaviors;
using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Behaviors;

public class TransactionBehaviorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset FrozenInstant =
        DateTimeOffset.Parse("2026-05-12T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Query_request_does_not_open_transactionAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        await using var ctx = CreateContext(clock);
        var behavior = new TransactionBehavior<TestQuery, Result>(ctx);

        var txObservedInsideHandler = true;

        MessageHandlerDelegate<TestQuery, Result> next = (_, _) =>
        {
            txObservedInsideHandler = ctx.Database.CurrentTransaction is not null;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(
            new TestQuery(),
            next,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(txObservedInsideHandler);
        Assert.Null(ctx.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Command_success_commits_transactionAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        var studyId = StudyId.New();

        await using (var ctx = CreateContext(clock))
        {
            var behavior = new TransactionBehavior<TestCommand, Result>(ctx);

            MessageHandlerDelegate<TestCommand, Result> next = async (_, ct) =>
            {
                ctx.Studies.Add(StudyBuilder.Create(clock, studyId));
                await ctx.SaveChangesAsync(ct);
                return Result.Success();
            };

            var result = await behavior.Handle(
                new TestCommand(),
                next,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        await using var verify = CreateContext(clock);
        var persisted = await verify.Studies
            .FirstOrDefaultAsync(s => s.Id == studyId, TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task Command_failure_rollbacks_transactionAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        var studyId = StudyId.New();

        await using (var ctx = CreateContext(clock))
        {
            var behavior = new TransactionBehavior<TestCommand, Result>(ctx);

            MessageHandlerDelegate<TestCommand, Result> next = async (_, ct) =>
            {
                ctx.Studies.Add(StudyBuilder.Create(clock, studyId));
                await ctx.SaveChangesAsync(ct);
                return Result.Failure(Error.Validation("test.fail", "boom"));
            };

            var result = await behavior.Handle(
                new TestCommand(),
                next,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure);
            Assert.Equal("test.fail", result.Error.Code);
        }

        await using var verify = CreateContext(clock);
        var persisted = await verify.Studies
            .FirstOrDefaultAsync(s => s.Id == studyId, TestContext.Current.CancellationToken);

        Assert.Null(persisted);
    }

    [Fact]
    public async Task Command_exception_rollbacks_and_rethrowsAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        var studyId = StudyId.New();
        var boom = new InvalidOperationException("boom");

        await using (var ctx = CreateContext(clock))
        {
            var behavior = new TransactionBehavior<TestCommand, Result>(ctx);

            MessageHandlerDelegate<TestCommand, Result> next = async (_, ct) =>
            {
                ctx.Studies.Add(StudyBuilder.Create(clock, studyId));
                await ctx.SaveChangesAsync(ct);
                throw boom;
            };

            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(new TestCommand(), next, TestContext.Current.CancellationToken).AsTask());

            Assert.Same(boom, thrown);
        }

        await using var verify = CreateContext(clock);
        var persisted = await verify.Studies
            .FirstOrDefaultAsync(s => s.Id == studyId, TestContext.Current.CancellationToken);

        Assert.Null(persisted);
    }

    public sealed record TestCommand : IAppCommand;

    public sealed record TestQuery : IRequest<Result>;
}