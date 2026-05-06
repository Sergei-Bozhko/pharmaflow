using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Audit;

public sealed class AuditEvent
{
    public AuditEventId Id { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public UserId ActorUserId { get; private set; }
    public string ActorRoleAtTime { get; private set; } = default!;
    public AuditEventType EventType { get; private set; }
    public string TargetEntityType { get; private set; } = default!;
    public string TargetEntityId { get; private set; } = default!;
    public string? BeforeStateJson { get; private set; } = default!;
    public string? AfterStateJson { get; private set; } = default!;
    public string? ReasonForChange { get; private set; } = default!;
    public string? SourceIpAddress { get; private set; } = default!;
    public string? ClientInfo { get; private set; } = default!;
    public string EventPayloadHash { get; private set; } = default!;
    public string? PreviousEventHash { get; private set; } = default!;

    private AuditEvent() { }

    private AuditEvent(
        DateTimeOffset occurredAt,
        UserId actorUserId,
        string actorRoleAtTime,
        AuditEventType eventType,
        string targetEntityType,
        string targetEntityId,
        string? beforeStateJson,
        string? afterStateJson,
        string? reasonForChange,
        string? sourceIpAddress,
        string? clientInfo,
        string eventPayloadHash,
        string? previousEventHash)
    {
        Id = AuditEventId.Empty;
        OccurredAt = occurredAt;
        ActorUserId = actorUserId;
        ActorRoleAtTime = actorRoleAtTime;
        EventType = eventType;
        TargetEntityType = targetEntityType;
        TargetEntityId = targetEntityId;
        BeforeStateJson = beforeStateJson;
        AfterStateJson = afterStateJson;
        ReasonForChange = reasonForChange;
        SourceIpAddress = sourceIpAddress;
        ClientInfo = clientInfo;
        EventPayloadHash = eventPayloadHash;
        PreviousEventHash = previousEventHash;
    }

    public static Result<AuditEvent> Create(
        DateTimeOffset occurredAt,
        UserId actorUserId,
        string actorRoleAtTime,
        AuditEventType eventType,
        string targetEntityType,
        string targetEntityId,
        string? beforeStateJson,
        string? afterStateJson,
        string? reasonForChange,
        string? sourceIpAddress,
        string? clientInfo,
        string eventPayloadHash,
        string? previousEventHash,
        IClock clock
    )
    {
        if (actorUserId == UserId.Empty)
        {
            return Error.Validation(
                "audit_event.actor_user_id.required",
                "ActorUserId is required."
            );
        }

        if (string.IsNullOrWhiteSpace(actorRoleAtTime))
        {
            return Error.Validation(
                "audit_event.actor_role_at_time.required",
                "ActorRoleAtTime must be non-empty string."
            );
        }

        if (!Enum.IsDefined(eventType))
        {
            return Error.Validation(
                "audit_event.event_type.invalid",
                "EventType is invalid."
            );
        }

        if (string.IsNullOrWhiteSpace(targetEntityType))
        {
            return Error.Validation(
                "audit_event.target_entity_type.required",
                "TargetEntityType must be non-empty string."
            );
        }

        if (string.IsNullOrWhiteSpace(targetEntityId))
        {
            return Error.Validation(
                "audit_event.target_entity_id.required",
                "TargetEntityId must be non-empty string."
            );
        }

        if (string.IsNullOrWhiteSpace(eventPayloadHash))
        {
            return Error.Validation(
                "audit_event.event_payload_hash.required",
                "EventPayloadHash must be non-empty string."
            );
        }

        if (eventPayloadHash.Length != 64 ||
            !eventPayloadHash.All(char.IsAsciiHexDigit))
        {
            return Error.Validation(
                "audit_event.event_payload_hash.invalid",
                "EventPayloadHash must be 64-char hex."
            );
        }

        var auditEvent = new AuditEvent(
            occurredAt,
            actorUserId,
            actorRoleAtTime,
            eventType,
            targetEntityType,
            targetEntityId,
            beforeStateJson,
            afterStateJson,
            reasonForChange,
            sourceIpAddress,
            clientInfo,
            eventPayloadHash,
            previousEventHash
        );
        return auditEvent;
    }
}