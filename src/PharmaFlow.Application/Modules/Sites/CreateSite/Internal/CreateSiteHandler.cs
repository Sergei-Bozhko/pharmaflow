using Mediator;

using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Application.Modules.Sites.CreateSite.Internal;

internal sealed class CreateSiteHandler(IStudiesModule studiesModule, ISitesDbContext ctx, IClock clock)
    : IRequestHandler<CreateSiteCommand, Result<SiteId>>
{
    public async ValueTask<Result<SiteId>> Handle(CreateSiteCommand cmd, CancellationToken ct)
    {
        if (!await studiesModule.StudyExistsAsync(cmd.StudyId, ct))
        {
            return Error.NotFound("study.not_found", $"Study {cmd.StudyId} not found.");
        }

        var result = Site.Create(
            SiteId.New(),
            cmd.StudyId,
            cmd.SiteNumber,
            cmd.Name,
            cmd.Country,
            cmd.PrincipalInvestigatorUserId,
            clock);

        if (result.IsFailure)
        {
            return result.Error;
        }

        ctx.Sites.Add(result.Value);
        await ctx.SaveChangesAsync(ct);

        return result.Value.Id;
    }
}