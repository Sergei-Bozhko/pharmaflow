using Mediator;

using PharmaFlow.Application.Common.Mediator;
using PharmaFlow.Domain.Common;
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

internal sealed class CreateStudyHandler : IRequestHandler<CreateStudyCommand, Result<StudyId>>
{
    public ValueTask<Result<StudyId>> Handle(CreateStudyCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException(
            "CreateStudyHandler is stubbed for PFL-043; real impl lands in PFL-050.");
    }
}