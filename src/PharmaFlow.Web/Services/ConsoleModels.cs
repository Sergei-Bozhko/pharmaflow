namespace PharmaFlow.Web.Services;

// View/transport records mirroring the Api's JSON shapes. The console talks to the Api purely over
// HTTP, so it owns its own copies rather than referencing the Api/Application types.

public sealed record StudyRow(
    Guid Id, string ProtocolNumber, string Title, string Phase, string Status,
    string SponsorOrganization, DateTimeOffset CreatedAt);

public sealed record SiteRow(
    Guid Id, Guid StudyId, string SiteNumber, string Name, string Country, string Status,
    DateTimeOffset CreatedAt);

public sealed record OutboxRow(
    Guid Id, string Type, DateTimeOffset OccurredOn, DateTimeOffset? ProcessedOn, int Attempts, string? Error);

public sealed record InboxRow(Guid MessageId, DateTimeOffset ReceivedAt);

public sealed record KnownStudyRow(Guid StudyId, DateTimeOffset RegisteredAt);

public sealed record AuditRow(
    long Id, DateTimeOffset OccurredAt, Guid ActorUserId, string EventType,
    string TargetEntityType, string TargetEntityId, string? ReasonForChange);

public sealed record TransportState(string Transport);

public sealed record CreateStudyRequest(
    string ProtocolNumber, string Title, int Phase, string TherapeuticArea,
    string SponsorOrganization, int PlannedEnrolment, DateOnly PlannedStartDate, DateOnly PlannedEndDate);

public sealed record CreateSiteRequest(
    Guid StudyId, string SiteNumber, string Name, string Country, Guid PrincipalInvestigatorUserId);