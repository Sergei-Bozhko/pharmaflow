using Mediator;

using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Application.Common.Persistence;
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
) : IIdempotentAppCommand<StudyId>
{

}

internal sealed class CreateStudyHandler(IAppDbContext ctx, IClock clock)
    : IRequestHandler<CreateStudyCommand, Result<StudyId>>
{
    public async ValueTask<Result<StudyId>> Handle(CreateStudyCommand cmd, CancellationToken ct)
    {
        var result = Study.Create(
            StudyId.New(),
            cmd.ProtocolNumber,
            cmd.Title,
            cmd.Phase,
            cmd.TherapeuticArea,
            cmd.SponsorOrganization,
            cmd.PlannedEnrolment,
            cmd.PlannedStartDate,
            cmd.PlannedEndDate,
            clock
        );

        if (result.IsFailure)
        {
            return result.Error;
        }

        ctx.Studies.Add(result.Value);
        await ctx.SaveChangesAsync(ct);

        return result.Value.Id;
    }
}