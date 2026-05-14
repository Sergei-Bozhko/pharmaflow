using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Application.Common.Idempotency;
using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Behaviors;

public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsIdempotent = ComputeIsIdempotent();
    private static readonly Type? ResultValueType = ComputeResultValueType();

    private readonly IAppDbContext _ctx;
    private readonly IIdempotencyKeyProvider _keyProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public IdempotencyBehavior(
          IAppDbContext ctx,
          IIdempotencyKeyProvider keyProvider,
          ICurrentUser currentUser,
          IClock clock)
    {
        _ctx = ctx;
        _keyProvider = keyProvider;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        // Branch 1: not idempotent → straight pass-through.
        if (!IsIdempotent)
        {
            return await next(message, cancellationToken);
        }

        var key = _keyProvider.GetKey();

        // Branch 2: no HTTP context → pass-through (non-HTTP flow: CLI/test).
        if (key is null)
        {
            return await next(message, cancellationToken);
        }

        // Branch 3: HTTP context present but header missing/empty → validation failure.
        if (string.IsNullOrWhiteSpace(key))
        {
            return CreateFailure(Error.Validation(
                "idempotency.key_required",
                "Idempotency-Key header required for this request."));
        }

        var userId = _currentUser.UserId.Value;
        var requestHash = ComputeRequestHash(message);
        var now = _clock.UtcNow;

        var record = await _ctx.IdempotencyRecords
            .FirstOrDefaultAsync(
                r => r.Key == key && r.UserId == userId && r.ExpiresAt > now,
                cancellationToken);

        if (record is not null)
        {
            // Branch 5: hit + different hash → conflict.
            if (record.RequestHash != requestHash)
            {
                return CreateFailure(Error.Conflict(
                    "idempotency.body_mismatch",
                    "Same Idempotency-Key was used with a different request body."));
            }

            // Branch 4: hit + matching hash → cached replay (no next() call).
            return ReconstructSuccess(record.ResponseBody);
        }

        // Branch 6: miss → execute handler.
        var response = await next(message, cancellationToken);

        // Persist only on Result.Success. Failures and exceptions = not idempotent.
        if (response is Result r && r.IsSuccess)
        {
            var cachedBody = ExtractValueJson(response);
            var newRecord = IdempotencyRecord.Create(
                key,
                userId,
                requestHash,
                responseStatus: 200,                   // HTTP code unknown at this layer (Sprint 6 ProblemDetails maps real codes)
                responseBody: cachedBody,
                expiresAt: now.AddHours(24));

            _ctx.IdempotencyRecords.Add(newRecord);
            await _ctx.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    private static string ComputeRequestHash(TRequest message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);  // 64-char uppercase hex
    }

    private static string ExtractValueJson(TResponse response)
    {
        if (ResultValueType is null)
        {
            // TResponse == Result (non-generic). No value to cache.
            return string.Empty;
        }

        // TResponse == Result<T>. Read Value via reflection on the closed type.
        var valueProp = typeof(TResponse).GetProperty(nameof(Result<int>.Value))!;
        var value = valueProp.GetValue(response);
        return JsonSerializer.Serialize(value);
    }

    private static TResponse ReconstructSuccess(string cachedBody)
    {
        if (ResultValueType is null)
        {
            // TResponse == Result.
            return (TResponse)(object)Result.Success();
        }

        // TResponse == Result<T>. Deserialize T, then call Result<T>.Success(value).
        var value = JsonSerializer.Deserialize(cachedBody, ResultValueType);
        var success = typeof(TResponse).GetMethod(
            nameof(Result<int>.Success),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [ResultValueType],
            modifiers: null)!;
        return (TResponse)success.Invoke(null, [value])!;
    }

    private static TResponse CreateFailure(Error error)
    {
        if (ResultValueType is null)
        {
            return (TResponse)(object)Result.Failure(error);
        }

        // Result<T>.Failure(Error) — same reflection trick as ValidationBehavior.
        var failure = typeof(TResponse).GetMethod(
            nameof(Result.Failure),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(Error)],
            modifiers: null)!;
        return (TResponse)failure.Invoke(null, [error])!;
    }

    private static bool ComputeIsIdempotent()
    {
        var type = typeof(TRequest);
        if (typeof(IIdempotentAppCommand).IsAssignableFrom(type)) return true;
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIdempotentAppCommand<>));
    }

    private static Type? ComputeResultValueType()
    {
        var t = typeof(TResponse);
        return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Result<>)
            ? t.GetGenericArguments()[0]
            : null;
    }
}