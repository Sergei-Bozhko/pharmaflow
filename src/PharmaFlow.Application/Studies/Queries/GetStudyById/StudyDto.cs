using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Studies.Queries.GetStudyById;

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