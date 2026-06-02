using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Modules.Studies.Contracts;

public sealed record StudyDto
(
    Guid Id,
    string ProtocolNumber,
    string Title,
    StudyPhase Phase,
    string TherapeuticArea,
    string SponsorOrganization,
    int PlannedEnrolment,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    StudyStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);