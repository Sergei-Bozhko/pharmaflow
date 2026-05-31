using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Studies.Queries.GetStudyById;

internal sealed class GetStudyByIdHandler(IAppDbContext ctx)
    : IRequestHandler<GetStudyByIdQuery, Result<StudyDto>>
{
    public async ValueTask<Result<StudyDto>> Handle(
        GetStudyByIdQuery query,
        CancellationToken ct)
    {
        var dto = await ctx.Studies
            .AsNoTracking()
            .Where(s => s.Id == query.Id)
            .Select(s => new StudyDto(
                s.Id.Value,
                s.ProtocolNumber,
                s.Title,
                s.Phase,
                s.TherapeuticArea,
                s.SponsorOrganization,
                s.PlannedEnrolment,
                s.PlannedStartDate,
                s.PlannedEndDate,
                s.Status,
                s.CreatedAt,
                s.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Error.NotFound("study.not_found", $"Study with id {query.Id} not found.");
        }

        return dto;
    }
}