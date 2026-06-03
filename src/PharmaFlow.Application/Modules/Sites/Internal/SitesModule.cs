using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Sites.Contracts;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Sites.Internal;

internal sealed class SitesModule(ISitesDbContext ctx) : ISitesModule
{
    public Task<int> CountSitesForStudyAsync(StudyId studyId, CancellationToken ct)
    {
        return ctx.Sites
            .CountAsync(s => s.StudyId == studyId, ct);
    }
}