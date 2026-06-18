using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;
using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Api.Endpoints.Internal;

public static class IntegrationEventEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEvents(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("internal/integration-events").WithTags("Integration");

        group.MapPost("study-created", async (StudyCreatedEnvelope env,
                                            ISitesDbContext ctx,
                                            IClock clock,
                                            CancellationToken ct) =>
            {
                if (await ctx.InboxMessages.AnyAsync(i => i.MessageId == env.MessageId, ct))
                    return Results.Ok();

                var dto = JsonSerializer.Deserialize<StudyCreatedTransportDto>(env.Payload,
                    OutboxSerialization.Options)!;
                ctx.InboxMessages.Add(new InboxMessage(env.MessageId, clock.UtcNow));

                if (!await ctx.KnownStudies.AnyAsync(k => k.StudyId == dto.StudyId, ct))
                    ctx.KnownStudies.Add(StudyCreatedAcl.ToKnownStudy(dto, clock));

                await ctx.SaveChangesAsync(ct);
                return Results.Ok();
            });

        return app;
    }
}