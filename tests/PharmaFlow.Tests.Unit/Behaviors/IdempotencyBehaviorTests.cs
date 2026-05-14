using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Application.Common.Behaviors;
using PharmaFlow.Application.Common.Idempotency;
using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Tests.Unit.Behaviors;

public class IdempotencyBehaviorTests
{
    private static readonly DateTimeOffset FrozenInstant =
        new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TestUserGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Branch 1: not idempotent → pass-through.
    [Fact]
    public async Task Non_idempotent_request_bypasses_behaviorAsync()
    {
        var (ctx, behavior) = BuildHarness<TestQuery, Result>(keyOverride: "any-key");

        var called = false;
        MessageHandlerDelegate<TestQuery, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestQuery(), next, TestContext.Current.CancellationToken);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    // Branch 2: null key (no HTTP context) → pass-through.
    [Fact]
    public async Task Null_key_passes_through_to_handlerAsync()
    {
        var (ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: null);

        var called = false;
        MessageHandlerDelegate<TestIdempotentCommand, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestIdempotentCommand("p"), next, TestContext.Current.CancellationToken);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    // Branch 3: empty key (HTTP, header missing) → validation failure (non-generic Result).
    [Fact]
    public async Task Empty_key_returns_key_required_for_non_generic_resultAsync()
    {
        var (ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: "");

        var called = false;
        MessageHandlerDelegate<TestIdempotentCommand, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestIdempotentCommand("p"), next, TestContext.Current.CancellationToken);

        Assert.False(called);
        Assert.True(result.IsFailure);
        Assert.Equal("idempotency.key_required", result.Error.Code);
    }

    // Branch 3 + generic CreateFailure reflection branch.
    [Fact]
    public async Task Empty_key_returns_typed_failure_for_generic_resultAsync()
    {
        var (ctx, behavior) = BuildHarness<TestIdempotentCommandWithValue, Result<Guid>>(keyOverride: "");

        MessageHandlerDelegate<TestIdempotentCommandWithValue, Result<Guid>> next =
            (_, _) => ValueTask.FromResult(Result<Guid>.Success(Guid.NewGuid()));

        var result = await behavior.Handle(
            new TestIdempotentCommandWithValue("p"),
            next,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("idempotency.key_required", result.Error.Code);
        Assert.IsType<Result<Guid>>(result);   // reflection produced typed wrapper
    }

    // ---------- helpers ----------

    private static (AppDbContext ctx, IdempotencyBehavior<TRequest, TResponse> behavior)
        BuildHarness<TRequest, TResponse>(string? keyOverride)
        where TRequest : IRequest<TResponse>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"idem-unit-{Guid.NewGuid()}")
            .Options;
        var ctx = new AppDbContext(options);

        var clock = new FrozenTestClock(FrozenInstant);
        var user = new TestCurrentUser(new UserId(TestUserGuid));
        var keyProvider = new FixedKeyProvider(keyOverride);

        var behavior = new IdempotencyBehavior<TRequest, TResponse>(ctx, keyProvider, user, clock);
        return (ctx, behavior);
    }

    private sealed class FixedKeyProvider(string? key) : IIdempotencyKeyProvider
    {
        public string? GetKey() => key;
    }

    private sealed record TestCurrentUser(UserId UserId) : ICurrentUser
    {
        public string RoleAtTime => "TestRole";
    }

    private sealed class FrozenTestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    public sealed record TestQuery : IRequest<Result>;
    public sealed record TestIdempotentCommand(string Payload) : IIdempotentAppCommand;
    public sealed record TestIdempotentCommandWithValue(string Payload) : IIdempotentAppCommand<Guid>;
}