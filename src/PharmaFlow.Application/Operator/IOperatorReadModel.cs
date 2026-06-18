namespace PharmaFlow.Application.Operator;

// A flat, read-only projection over the whole system for the operator console (CQRS read side).
// Returns plain DTOs — no module Internal types cross this boundary — so it stays clear of the
// module arch gate. The console's observability views read through this; commands still go via Mediator.
public interface IOperatorReadModel
{
    Task<IReadOnlyList<StudyRow>> StudiesAsync(CancellationToken ct);
    Task<IReadOnlyList<SiteRow>> SitesAsync(Guid? studyId, CancellationToken ct);
    Task<IReadOnlyList<OutboxRow>> OutboxAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<InboxRow>> InboxAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<KnownStudyRow>> KnownStudiesAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<AuditRow>> AuditAsync(int take, CancellationToken ct);
}

public sealed record StudyRow(
    Guid Id,
    string ProtocolNumber,
    string Title,
    string Phase,
    string Status,
    string SponsorOrganization,
    DateTimeOffset CreatedAt);

public sealed record SiteRow(
    Guid Id,
    Guid StudyId,
    string SiteNumber,
    string Name,
    string Country,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record OutboxRow(
    Guid Id,
    string Type,
    DateTimeOffset OccurredOn,
    DateTimeOffset? ProcessedOn,
    int Attempts,
    string? Error);

public sealed record InboxRow(
    Guid MessageId,
    DateTimeOffset ReceivedAt);

public sealed record KnownStudyRow(
    Guid StudyId,
    DateTimeOffset RegisteredAt);

public sealed record AuditRow(
    long Id,
    DateTimeOffset OccurredAt,
    Guid ActorUserId,
    string EventType,
    string TargetEntityType,
    string TargetEntityId,
    string? ReasonForChange);