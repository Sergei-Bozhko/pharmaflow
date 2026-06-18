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
        const string expectedType = "StudyCreated";

        var group = app.MapGroup("internal/integration-events").WithTags("Integration");

        group.MapPost("study-created", async (StudyCreatedEnvelope env,
                                            ISitesDbContext ctx,
                                            IClock clock,
                                            CancellationToken ct) =>
            {
                // This endpoint speaks one contract. A foreign type is a producer/consumer
                // mismatch, not a transient fault — reject it rather than mis-deserialize.
                if (env.Type != expectedType)
                    return Results.BadRequest($"Unsupported event type '{env.Type}'.");

                // Inbox dedup — the cross-boundary replacement for the producer's processed_on,
                // which the consumer can't see over HTTP. A seen id is a no-op (already committed).
                if (await ctx.InboxMessages.AnyAsync(i => i.MessageId == env.MessageId, ct))
                    return Results.Ok();

                StudyCreatedTransportDto? dto;
                try
                {
                    dto = JsonSerializer.Deserialize<StudyCreatedTransportDto>(env.Payload,
                        OutboxSerialization.Options);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Malformed payload.");
                }

                if (dto is null)
                    return Results.BadRequest("Empty payload.");

                ctx.InboxMessages.Add(new InboxMessage(env.MessageId, clock.UtcNow));

                // Natural-key guard stays as the second backstop (PFL-061): the inbox catches a
                // redelivered message id; this catches a distinct path to the same study.
                if (!await ctx.KnownStudies.AnyAsync(k => k.StudyId == dto.StudyId, ct))
                    ctx.KnownStudies.Add(StudyCreatedAcl.ToKnownStudy(dto, clock));

                try
                {
                    // Inbox row + KnownStudy commit together — durably accepted before the 2xx,
                    // so any non-2xx tells the producer to retry (at-least-once over HTTP).
                    await ctx.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    // A concurrent delivery of the same message id lost the inbox-PK race.
                    // The PK is the backstop; treat it as already-accepted, not a 500.
                    return Results.Ok();
                }

                return Results.Ok();
            });

        return app;
    }
}