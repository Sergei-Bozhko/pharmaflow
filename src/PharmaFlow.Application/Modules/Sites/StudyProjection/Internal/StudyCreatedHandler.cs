using Mediator;

using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Studies.Contracts;

namespace PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;

internal sealed class StudyCreatedHandler(ISitesDbContext ctx)
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

        ctx.KnownStudies.Add(new KnownStudy(notification.StudyId, notification.OccurredAt));
        await ctx.SaveChangesAsync(cancellationToken);
    }
}