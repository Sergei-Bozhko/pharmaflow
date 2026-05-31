using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Studies.Commands.CreateStudy;

public record CreateStudyDto
(
    string ProtocolNumber,
    string Title,
    StudyPhase Phase,
    string TherapeuticArea,
    string SponsorOrganization,
    int PlannedEnrolment,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate
);