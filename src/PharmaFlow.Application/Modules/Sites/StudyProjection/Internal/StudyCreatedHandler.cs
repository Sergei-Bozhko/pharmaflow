using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;

internal sealed class StudyCreatedHandler(ISitesDbContext ctx, IClock clock)
    : INotificationHandler<StudyCreatedIntegrationEvent>
{
    public async ValueTask Handle(StudyCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var alreadyKnown = await ctx.KnownStudies
            .AnyAsync(k => k.StudyId == notification.StudyId, cancellationToken);

        if (alreadyKnown)
        {
            return;
        }

        // RegisteredAt is when this consumer learned of the study (its own clock), not the wire
        // OccurredAt — same semantic as the HTTP path's ACL, so both transports converge.
        ctx.KnownStudies.Add(new KnownStudy(notification.StudyId, clock.UtcNow));
        await ctx.SaveChangesAsync(cancellationToken);
    }
}