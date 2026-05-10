using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Infrastructure.Persistence.Interceptors;

public sealed class AuditingSaveChangesInterceptor(IClock clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    private const string PlaceholderEventPayloadHash = "0000000000000000000000000000000000000000000000000000000000000000";

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context!;
        var now = clock.UtcNow;
        var actor = currentUser.UserId;
        var role = currentUser.RoleAtTime;
        var actorString = actor.ToString();

        AuditEventType eventType;

        var auditTargets = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditedEntity)
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in auditTargets)
        {
            if (entry.State == EntityState.Added)
            {
                StampCreated(entry, now, actorString);
                eventType = AuditEventType.Create;
            }
            else
            {
                StampModified(entry, now, actorString);
                eventType = IsSoftDelete(entry) ? AuditEventType.SoftDelete : AuditEventType.Update;
            }

            var auditRow = BuildAuditEvent(entry, eventType, now, actor, role);
            context.Set<AuditEvent>().Add(auditRow);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampCreated(EntityEntry entry, DateTimeOffset now, string actorString)
    {
        entry.Property("CreatedAt").CurrentValue = now;
        entry.Property("CreatedBy").CurrentValue = actorString;
        entry.Property("UpdatedAt").CurrentValue = now;
        entry.Property("UpdatedBy").CurrentValue = actorString;
    }

    private static void StampModified(EntityEntry entry, DateTimeOffset now, string actorString)
    {
        entry.Property("UpdatedAt").CurrentValue = now;
        entry.Property("UpdatedBy").CurrentValue = actorString;
    }

    private static bool IsSoftDelete(EntityEntry entry)
    {
        var prop = entry.Property("IsDeleted");
        return prop.IsModified && prop.OriginalValue is false && prop.CurrentValue is true;
    }

    private static AuditEvent BuildAuditEvent(
        EntityEntry entry,
        AuditEventType eventType,
        DateTimeOffset now,
        UserId actor,
        string role)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = entry.Property("Id").CurrentValue!.ToString()!;

        var (beforeJson, afterJson) = eventType switch
        {
            AuditEventType.Create => (null, SerializeProperties(entry, isOriginal: false)),
            AuditEventType.Update => (SerializeProperties(entry, isOriginal: true), SerializeProperties(entry, isOriginal: false)),
            AuditEventType.SoftDelete => (SerializeProperties(entry, isOriginal: true), (string?)null),
            _ => (null, null),
        };

        var result = AuditEvent.Create(
            occurredAt: now,
            actorUserId: actor,
            actorRoleAtTime: role,
            eventType: eventType,
            targetEntityType: entityType,
            targetEntityId: entityId,
            beforeStateJson: beforeJson,
            afterStateJson: afterJson,
            reasonForChange: null,
            sourceIpAddress: null,
            clientInfo: null,
            eventPayloadHash: PlaceholderEventPayloadHash,
            previousEventHash: null);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"AuditEvent.Create failed for {entityType}#{entityId}: {result.Error}");
        }

        return result.Value;
    }



    private static string SerializeProperties(EntityEntry entry, bool isOriginal)
    {
        var snapshot = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            snapshot[property.Metadata.Name] = isOriginal ? property.OriginalValue : property.CurrentValue;
        }
        return JsonSerializer.Serialize(snapshot, SnapshotJsonOptions);
    }

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };
}