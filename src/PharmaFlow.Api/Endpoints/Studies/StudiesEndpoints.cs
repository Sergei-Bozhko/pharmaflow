using Mediator;

using PharmaFlow.Api.Common;
using PharmaFlow.Application.Studies.Commands.CreateStudy;
using PharmaFlow.Application.Studies.Queries.GetStudyById;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Api.Endpoints.Studies;

public static class StudiesEndpoints
{
    public static IEndpointRouteBuilder MapStudies(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/studies").WithTags("Studies");

        group.MapPost("", async (CreateStudyDto dto,
                                ISender sender,
                                HttpContext ctx,
                                CancellationToken ct) =>
        {
            var cmd = new CreateStudyCommand(
                dto.ProtocolNumber,
                dto.Title,
                dto.Phase,
                dto.TherapeuticArea,
                dto.SponsorOrganization,
                dto.PlannedEnrolment,
                dto.PlannedStartDate,
                dto.PlannedEndDate);

            var result = await sender.Send(cmd, ct);

            return result.ToCreatedResult(ctx, id => $"{ctx.Request.Path}/{id.Value}");
        });

        group.MapGet("{id:guid}", async (Guid id, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var query = new GetStudyByIdQuery(new StudyId(id));
            var result = await sender.Send(query, ct);

            return result.ToHttpResult(ctx);
        });

        return app;
    }
}