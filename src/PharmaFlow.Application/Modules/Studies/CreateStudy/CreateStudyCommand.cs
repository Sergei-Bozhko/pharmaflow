using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Modules.Studies.CreateStudy;

public sealed record CreateStudyCommand(
    string ProtocolNumber,
    string Title,
    StudyPhase Phase,
    string TherapeuticArea,
    string SponsorOrganization,
    int PlannedEnrolment,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate
) : IIdempotentAppCommand<StudyId>
{

}