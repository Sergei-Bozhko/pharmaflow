using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Modules.Studies.CreateStudy.Internal;

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