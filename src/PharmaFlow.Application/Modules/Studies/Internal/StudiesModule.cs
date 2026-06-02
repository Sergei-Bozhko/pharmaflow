using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Studies.Internal;

internal sealed class StudiesModule(IStudiesDbContext ctx) : IStudiesModule
{

    public Task<StudyDto?> GetStudyByIdAsync(StudyId studyId, CancellationToken ct) =>
        ctx.Studies
            .AsNoTracking()
            .Where(s => s.Id == studyId)
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

    public Task<bool> StudyExistsAsync(StudyId studyId, CancellationToken ct) =>
        ctx.Studies
            .AsNoTracking()
            .AnyAsync(s => s.Id == studyId, ct);
}