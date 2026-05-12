using PharmaFlow.Application.Common.Mediator;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Studies.Commands.CreateStudy;

public sealed record CreateStudyCommand(
    string ProtocolNumber,
    string Title,
    StudyPhase Phase,
    string TherapeuticArea,
    string SponsorOrganization,
    int PlannedEnrolment,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate
) : IIdempotentCommand<StudyId>
{
    
}