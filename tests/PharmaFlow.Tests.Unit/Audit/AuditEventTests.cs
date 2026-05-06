using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Audit;

public class AuditEventTests
{
    private static readonly FrozenClock Clock = new(
        new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero)
    );

    private const string ValidHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static Result<AuditEvent> NewValid(
        UserId? actorUserId = null,
        string actorRoleAtTime = "Investigator",
        AuditEventType eventType = AuditEventType.Create,
        string targetEntityType = "Study",
        string targetEntityId = "abc-123",
        string eventPayloadHash = ValidHash) =>
        AuditEvent.Create(
            occurredAt: Clock.UtcNow,
            actorUserId: actorUserId ?? UserId.New(),
            actorRoleAtTime: actorRoleAtTime,
            eventType: eventType,
            targetEntityType: targetEntityType,
            targetEntityId: targetEntityId,
            beforeStateJson: null,
            afterStateJson: """{"name":"X"}""",
            reasonForChange: "study activation",
            sourceIpAddress: "10.0.0.1",
            clientInfo: "Mozilla/5.0",
            eventPayloadHash: eventPayloadHash,
            previousEventHash: null,
            clock: Clock
        );

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_all_fields_populated()
    {
        var actor = UserId.New();

        var result = AuditEvent.Create(
            occurredAt: Clock.UtcNow,
            actorUserId: actor,
            actorRoleAtTime: "Sponsor",
            eventType: AuditEventType.Update,
            targetEntityType: "Site",
            targetEntityId: "site-42",
            beforeStateJson: """{"status":"Pending"}""",
            afterStateJson: """{"status":"Active"}""",
            reasonForChange: "site activation",
            sourceIpAddress: "192.168.1.1",
            clientInfo: "Chrome/130",
            eventPayloadHash: ValidHash,
            previousEventHash: null,
            clock: Clock
        );

        Assert.True(result.IsSuccess);
        var ae = result.Value;
        Assert.Equal(AuditEventId.Empty, ae.Id);
        Assert.Equal(Clock.UtcNow, ae.OccurredAt);
        Assert.Equal(actor, ae.ActorUserId);
        Assert.Equal("Sponsor", ae.ActorRoleAtTime);
        Assert.Equal(AuditEventType.Update, ae.EventType);
        Assert.Equal("Site", ae.TargetEntityType);
        Assert.Equal("site-42", ae.TargetEntityId);
        Assert.Equal("""{"status":"Pending"}""", ae.BeforeStateJson);
        Assert.Equal("""{"status":"Active"}""", ae.AfterStateJson);
        Assert.Equal("site activation", ae.ReasonForChange);
        Assert.Equal("192.168.1.1", ae.SourceIpAddress);
        Assert.Equal("Chrome/130", ae.ClientInfo);
        Assert.Equal(ValidHash, ae.EventPayloadHash);
        Assert.Null(ae.PreviousEventHash);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_empty_ActorUserId()
    {
        var result = NewValid(actorUserId: UserId.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.actor_user_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_ActorRoleAtTime()
    {
        var result = NewValid(actorRoleAtTime: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.actor_role_at_time.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_undefined_EventType()
    {
        var result = NewValid(eventType: (AuditEventType)999);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.event_type.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_TargetEntityType()
    {
        var result = NewValid(targetEntityType: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.target_entity_type.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_TargetEntityId()
    {
        var result = NewValid(targetEntityId: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.target_entity_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_EventPayloadHash()
    {
        var result = NewValid(eventPayloadHash: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.event_payload_hash.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_wrong_length_EventPayloadHash()
    {
        var result = NewValid(eventPayloadHash: "abc123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.event_payload_hash.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_non_hex_EventPayloadHash()
    {
        var nonHex = new string('z', 64);

        var result = NewValid(eventPayloadHash: nonHex);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("audit_event.event_payload_hash.invalid", result.Error.Code);
    }

    // --- Structure: append-only contract ---

    [Fact]
    public void AuditEvent_has_no_public_setters()
    {
        var setters = typeof(AuditEvent)
            .GetProperties()
            .Select(p => p.GetSetMethod(nonPublic: false))
            .Where(m => m is not null)
            .ToList();

        Assert.Empty(setters);
    }

    [Fact]
    public void AuditEvent_does_not_inherit_Entity()
    {
        Assert.Equal(typeof(object), typeof(AuditEvent).BaseType);
    }
}