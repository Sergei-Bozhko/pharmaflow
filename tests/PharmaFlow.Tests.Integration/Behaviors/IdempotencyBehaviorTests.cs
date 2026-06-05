using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Application.Common.Behaviors;
using PharmaFlow.Application.Common.Idempotency;
using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Behaviors;

public class IdempotencyBehaviorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset FrozenInstant =
        DateTimeOffset.Parse("2026-05-14T12:00:00Z", CultureInfo.InvariantCulture);

    private static readonly Guid TestUserGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ---------- Branch 1: non-idempotent request → pass-through ----------

    [Fact]
    public async Task Non_idempotent_request_bypasses_behaviorAsync()
    {
        var (clock, user, ctx, behavior) = BuildHarness<TestQuery, Result>(keyOverride: "any-key");

        var called = false;
        MessageHandlerDelegate<TestQuery, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestQuery(), next, TestContext.Current.CancellationToken);

        Assert.True(called);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, await ctx.IdempotencyRecords.CountAsync(TestContext.Current.CancellationToken));
    }

    // ---------- Branch 2: null key (non-HTTP) → pass-through ----------

    [Fact]
    public async Task Null_key_passes_through_to_handlerAsync()
    {
        var (clock, user, ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: null);

        var called = false;
        MessageHandlerDelegate<TestIdempotentCommand, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestIdempotentCommand("payload"), next, TestContext.Current.CancellationToken);

        Assert.True(called);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, await ctx.IdempotencyRecords.CountAsync(TestContext.Current.CancellationToken));
    }

    // ---------- Branch 3: empty key (HTTP, missing header) → validation failure ----------

    [Fact]
    public async Task Empty_key_returns_key_requiredAsync()
    {
        var (clock, user, ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: "");

        var called = false;
        MessageHandlerDelegate<TestIdempotentCommand, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestIdempotentCommand("payload"), next, TestContext.Current.CancellationToken);

        Assert.False(called);
        Assert.True(result.IsFailure);
        Assert.Equal("idempotency.key_required", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }

    // ---------- Branch 4: cache hit + matching hash → cached replay ----------

    [Fact]
    public async Task Cache_hit_matching_hash_replays_without_calling_handlerAsync()
    {
        var key = "key-replay";
        var message = new TestIdempotentCommand("payload-a");
        var hash = ComputeHashOf(message);

        await SeedRecordAsync(key, TestUserGuid, hash, responseBody: "null", expiresIn: TimeSpan.FromHours(1));

        var (clock, user, ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: key);

        var called = false;
        MessageHandlerDelegate<TestIdempotentCommand, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(message, next, TestContext.Current.CancellationToken);

        Assert.False(called);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await ctx.IdempotencyRecords.CountAsync(TestContext.Current.CancellationToken));
    }

    // ---------- Branch 5: cache hit + different hash → conflict ----------

    [Fact]
    public async Task Cache_hit_different_hash_returns_conflictAsync()
    {
        var key = "key-conflict";
        var otherHash = new string('A', 64);   // 64-char placeholder hex

        await SeedRecordAsync(key, TestUserGuid, otherHash, responseBody: "null", expiresIn: TimeSpan.FromHours(1));

        var (clock, user, ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: key);

        var called = false;
        MessageHandlerDelegate<TestIdempotentCommand, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestIdempotentCommand("payload-b"), next, TestContext.Current.CancellationToken);

        Assert.False(called);
        Assert.True(result.IsFailure);
        Assert.Equal("idempotency.body_mismatch", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    // ---------- Branch 6a: cache miss + success → persists record ----------

    [Fact]
    public async Task Cache_miss_calls_handler_and_persists_on_successAsync()
    {
        var key = "key-miss-success";
        var message = new TestIdempotentCommand("payload-c");
        var expectedHash = ComputeHashOf(message);

        var (clock, user, ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: key);

        MessageHandlerDelegate<TestIdempotentCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        var result = await behavior.Handle(message, next, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        await using var verify = CreateContext(new FrozenClock(FrozenInstant));
        var record = await verify.IdempotencyRecords.FirstOrDefaultAsync(
            r => r.Key == key && r.UserId == TestUserGuid, TestContext.Current.CancellationToken);

        Assert.NotNull(record);
        Assert.Equal(expectedHash, record.RequestHash);
        Assert.Equal(200, record.ResponseStatus);
        Assert.Equal(FrozenInstant.AddHours(24), record.ExpiresAt);
    }

    // ---------- Branch 6b: cache miss + failure → no record ----------

    [Fact]
    public async Task Cache_miss_does_not_persist_on_failureAsync()
    {
        var key = "key-miss-failure";
        var (clock, user, ctx, behavior) = BuildHarness<TestIdempotentCommand, Result>(keyOverride: key);

        MessageHandlerDelegate<TestIdempotentCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.Failure(Error.Validation("test.fail", "boom")));

        var result = await behavior.Handle(new TestIdempotentCommand("payload-d"), next, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("test.fail", result.Error.Code);

        await using var verify = CreateContext(new FrozenClock(FrozenInstant));
        var count = await verify.IdempotencyRecords.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, count);
    }

    // ---------- Generic Result<T> shape — exercises reflection branches ----------

    [Fact]
    public async Task Cache_miss_with_generic_result_persists_unwrapped_value_and_replays_itAsync()
    {
        var key = "key-generic";
        var message = new TestIdempotentCommandWithValue("payload-e");
        var returnedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Round 1: cache miss → persist record with unwrapped Guid as JSON.
        {
            var (_, _, _, behavior) = BuildHarness<TestIdempotentCommandWithValue, Result<Guid>>(keyOverride: key);

            MessageHandlerDelegate<TestIdempotentCommandWithValue, Result<Guid>> next =
                (_, _) => ValueTask.FromResult(Result<Guid>.Success(returnedId));

            var result = await behavior.Handle(message, next, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(returnedId, result.Value);
        }

        // Verify persisted body is the raw guid JSON (not the wrapper).
        await using var verify = CreateContext(new FrozenClock(FrozenInstant));
        var record = await verify.IdempotencyRecords.FirstOrDefaultAsync(
            r => r.Key == key, TestContext.Current.CancellationToken);
        Assert.NotNull(record);
        Assert.Equal(JsonSerializer.Serialize(returnedId), record.ResponseBody);

        // Round 2: same harness, same key → cache hit → replay reconstructs Result<Guid>.Success(returnedId).
        {
            var (_, _, _, behavior) = BuildHarness<TestIdempotentCommandWithValue, Result<Guid>>(keyOverride: key);

            var called = false;
            MessageHandlerDelegate<TestIdempotentCommandWithValue, Result<Guid>> next = (_, _) =>
            {
                called = true;
                return ValueTask.FromResult(Result<Guid>.Success(Guid.NewGuid()));
            };

            var result = await behavior.Handle(message, next, TestContext.Current.CancellationToken);

            Assert.False(called);
            Assert.True(result.IsSuccess);
            Assert.Equal(returnedId, result.Value);   // reconstructed from cached body
        }
    }

    [Fact]
    public async Task Empty_key_with_generic_result_returns_typed_failureAsync()
    {
        var (_, _, _, behavior) = BuildHarness<TestIdempotentCommandWithValue, Result<Guid>>(keyOverride: "");

        MessageHandlerDelegate<TestIdempotentCommandWithValue, Result<Guid>> next =
            (_, _) => ValueTask.FromResult(Result<Guid>.Success(Guid.NewGuid()));

        var result = await behavior.Handle(
            new TestIdempotentCommandWithValue("payload"),
            next,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("idempotency.key_required", result.Error.Code);
        Assert.IsType<Result<Guid>>(result);   // proves reflection branch produced typed wrapper
    }


    // ---------- helpers ----------

    private (FrozenClock clock, ICurrentUser user, AppDbContext ctx, IdempotencyBehavior<TRequest, TResponse> behavior)
        BuildHarness<TRequest, TResponse>(string? keyOverride)
        where TRequest : IRequest<TResponse>
    {
        var clock = new FrozenClock(FrozenInstant);
        var user = new TestCurrentUser(new UserId(TestUserGuid));
        var ctx = CreateContext(clock, user);
        var keyProvider = new FixedKeyProvider(keyOverride);
        var behavior = new IdempotencyBehavior<TRequest, TResponse>(ctx, keyProvider, user, clock);
        return (clock, user, ctx, behavior);
    }

    private async Task SeedRecordAsync(string key, Guid userId, string hash, string responseBody, TimeSpan expiresIn)
    {
        await using var seed = CreateContext(new FrozenClock(FrozenInstant));
        seed.IdempotencyRecords.Add(IdempotencyRecord.Create(
            key, userId, hash, 200, responseBody, FrozenInstant.Add(expiresIn)));
        await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string ComputeHashOf<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }

    private sealed class FixedKeyProvider(string? key) : IIdempotencyKeyProvider
    {
        public string? GetKey() => key;
    }

    private sealed record TestCurrentUser(UserId UserId) : ICurrentUser
    {
        public string RoleAtTime => "TestRole";
    }

    public sealed record TestQuery : IRequest<Result>;
    public sealed record TestIdempotentCommand(string Payload) : IIdempotentAppCommand;
    public sealed record TestIdempotentCommandWithValue(string Payload) : IIdempotentAppCommand<Guid>;
}