using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Application.Common.Behaviors;
using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Tests.Unit.Behaviors;

public class AuditBehaviorTests
{
    private static readonly DateTimeOffset FrozenInstant =
        new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TestUserGuid =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Successful_command_writes_audit_row_with_command_outcomeAsync()
    {
        var (ctx, behavior) = BuildHarness<TestCommand, Result>();

        MessageHandlerDelegate<TestCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        var result = await behavior.Handle(
            new TestCommand("payload"), next, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        var row = await ctx.AuditEvents
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AuditEventType.CommandOutcome, row.EventType);
        Assert.Equal("Command", row.TargetEntityType);
        Assert.Equal(nameof(TestCommand), row.TargetEntityId);
        Assert.Equal(new UserId(TestUserGuid), row.ActorUserId);
        Assert.Equal(FrozenInstant, row.OccurredAt);
        Assert.NotNull(row.AfterStateJson);
        Assert.Contains("\"outcome\":\"Success\"", row.AfterStateJson, StringComparison.Ordinal);
        Assert.Contains("\"errorCode\":null", row.AfterStateJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_command_writes_audit_row_with_error_codeAsync()
    {
        var (ctx, behavior) = BuildHarness<TestCommand, Result>();

        MessageHandlerDelegate<TestCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.Failure(Error.Validation("test.fail", "boom")));

        var result = await behavior.Handle(
            new TestCommand("payload"), next, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("test.fail", result.Error.Code);

        var row = await ctx.AuditEvents
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AuditEventType.CommandOutcome, row.EventType);
        Assert.NotNull(row.AfterStateJson);
        Assert.Contains("\"outcome\":\"Failure\"", row.AfterStateJson, StringComparison.Ordinal);
        Assert.Contains("\"errorCode\":\"test.fail\"", row.AfterStateJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Exception_writes_audit_row_then_rethrowsAsync()
    {
        var (ctx, behavior) = BuildHarness<TestCommand, Result>();
        var boom = new InvalidOperationException("boom");

        MessageHandlerDelegate<TestCommand, Result> next = (_, _) => throw boom;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new TestCommand("payload"), next, TestContext.Current.CancellationToken).AsTask());

        Assert.Same(boom, thrown);

        var row = await ctx.AuditEvents
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AuditEventType.CommandOutcome, row.EventType);
        Assert.NotNull(row.AfterStateJson);
        Assert.Contains("\"outcome\":\"Exception\"", row.AfterStateJson, StringComparison.Ordinal);
        Assert.Contains("\"errorCode\":\"InvalidOperationException\"", row.AfterStateJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Query_request_is_not_auditedAsync()
    {
        var (ctx, behavior) = BuildHarness<TestQuery, Result>();

        MessageHandlerDelegate<TestQuery, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        var result = await behavior.Handle(
            new TestQuery(), next, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await ctx.AuditEvents.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Generic_command_with_result_value_writes_audit_rowAsync()
    {
        var (ctx, behavior) = BuildHarness<TestCommandWithValue, Result<Guid>>();
        var returnedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        MessageHandlerDelegate<TestCommandWithValue, Result<Guid>> next =
            (_, _) => ValueTask.FromResult(Result<Guid>.Success(returnedId));

        var result = await behavior.Handle(
            new TestCommandWithValue("payload"), next, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(returnedId, result.Value);

        var row = await ctx.AuditEvents
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(nameof(TestCommandWithValue), row.TargetEntityId);
        Assert.NotNull(row.AfterStateJson);
        Assert.Contains("\"outcome\":\"Success\"", row.AfterStateJson, StringComparison.Ordinal);
    }

    private static (AppDbContext ctx, AuditBehavior<TRequest, TResponse> behavior)
        BuildHarness<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"audit-unit-{Guid.NewGuid()}")
            .Options;
        var ctx = new AppDbContext(options);

        var clock = new FrozenTestClock(FrozenInstant);
        var user = new FakeCurrentUser(new UserId(TestUserGuid));

        var behavior = new AuditBehavior<TRequest, TResponse>(ctx, clock, user);
        return (ctx, behavior);
    }

    private sealed record FakeCurrentUser(UserId UserId) : ICurrentUser
    {
        public string RoleAtTime => "TestRole";
    }

    private sealed class FrozenTestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    public sealed record TestQuery : IRequest<Result>;
    public sealed record TestCommand(string Payload) : IAppCommand;
    public sealed record TestCommandWithValue(string Payload) : IAppCommand<Guid>;
}