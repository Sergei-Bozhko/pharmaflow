using Mediator;

using PharmaFlow.Api.Common;
using PharmaFlow.Application.Modules.Sites.CreateSite;
using PharmaFlow.Application.Modules.Sites.CreateSite.Internal;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Api.Endpoints.Sites;

public static class SitesEndpoints
{
    public static IEndpointRouteBuilder MapSites(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/sites").WithTags("Sites");

        group.MapPost("", async (CreateSiteDto dto,
                                ISender sender,
                                HttpContext ctx,
                                CancellationToken ct) =>
        {
            var cmd = new CreateSiteCommand(
                new StudyId(dto.StudyId),
                dto.SiteNumber,
                dto.Name,
                dto.Country,
                new UserId(dto.PrincipalInvestigatorUserId));

            var result = await sender.Send(cmd, ct);

            return result.ToCreatedResult(ctx, id => $"{ctx.Request.Path}/{id.Value}");
        });

        return app;
    }
}