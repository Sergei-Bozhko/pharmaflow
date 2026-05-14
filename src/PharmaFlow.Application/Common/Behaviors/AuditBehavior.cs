using System.Diagnostics;
using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string PlaceholderEventPayloadHash =
        "0000000000000000000000000000000000000000000000000000000000000000";

    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static readonly bool IsCommand = ComputeIsCommand();

    private readonly IAppDbContext _ctx;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AuditBehavior(IAppDbContext ctx, IClock clock, ICurrentUser currentUser)
    {
        _ctx = ctx;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand)
        {
            return await next(message, cancellationToken);
        }

        var commandName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next(message, cancellationToken);
            sw.Stop();

            var (outcome, errorCode) = response is Result r && r.IsFailure
                ? ("Failure", r.Error.Code)
                : ("Success", (string?)null);

            await TryPersistAuditRowAsync(
                message, commandName, outcome, errorCode, sw.ElapsedMilliseconds, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            await TryPersistAuditRowAsync(
                message, commandName, "Exception", ex.GetType().Name, sw.ElapsedMilliseconds, cancellationToken);
            throw;
        }
    }

    private async Task TryPersistAuditRowAsync(
        TRequest message,
        string commandName,
        string outcome,
        string? errorCode,
        long elapsedMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                command = message,
                outcome,
                errorCode,
                elapsedMs,
            };
            var afterStateJson = JsonSerializer.Serialize(payload, SnapshotOptions);

            var auditEventResult = AuditEvent.Create(
                occurredAt: _clock.UtcNow,
                actorUserId: _currentUser.UserId,
                actorRoleAtTime: _currentUser.RoleAtTime,
                eventType: AuditEventType.CommandOutcome,
                targetEntityType: "Command",
                targetEntityId: commandName,
                beforeStateJson: null,
                afterStateJson: afterStateJson,
                reasonForChange: null,
                sourceIpAddress: null,
                clientInfo: null,
                eventPayloadHash: PlaceholderEventPayloadHash,
                previousEventHash: null);

            if (auditEventResult.IsFailure)
            {
                return;
            }

            var dbContext = (DbContext)_ctx;
            dbContext.Set<AuditEvent>().Add(auditEventResult.Value);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort save per PFL-046 §64. Never fail the request because the
            // command-outcome audit row couldn't write.
        }
    }

    private static bool ComputeIsCommand()
    {
        var type = typeof(TRequest);
        if (typeof(IAppCommand).IsAssignableFrom(type))
        {
            return true;
        }
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAppCommand<>));
    }
}