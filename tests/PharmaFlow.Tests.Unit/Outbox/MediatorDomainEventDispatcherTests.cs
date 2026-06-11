using Mediator;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Sites.Events;
using PharmaFlow.Domain.Studies.Events;
using PharmaFlow.Infrastructure.Outbox;

namespace PharmaFlow.Tests.Unit.Outbox;

// PFL-061 dispatch seam. The processor hands the dispatcher a *domain* event; the dispatcher
// maps it to the published cross-module *integration* event. Publishing is opt-in per event:
// only events with a mapping arm cross the boundary.
public class MediatorDomainEventDispatcherTests
{
    private static readonly DateTimeOffset Occurred =
        new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StudyCreated_is_mapped_and_published_onceAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var publisher = new RecordingPublisher();
        var dispatcher = new MediatorDomainEventDispatcher(publisher);
        var studyId = StudyId.New();

        await dispatcher.DispatchAsync(new StudyCreated(studyId, Occurred), ct);

        var published = Assert.Single(publisher.Published);
        var integrationEvent = Assert.IsType<StudyCreatedIntegrationEvent>(published);
        Assert.Equal(studyId.Value, integrationEvent.StudyId);
        Assert.Equal(Occurred, integrationEvent.OccurredAt);
    }

    [Fact]
    public async Task A_domain_event_with_no_mapping_publishes_nothingAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var publisher = new RecordingPublisher();
        var dispatcher = new MediatorDomainEventDispatcher(publisher);

        await dispatcher.DispatchAsync(new SiteCreated(SiteId.New(), StudyId.New(), Occurred), ct);

        Assert.Empty(publisher.Published);
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<INotification> Published { get; } = [];

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification);
            return ValueTask.CompletedTask;
        }

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add((INotification)notification);
            return ValueTask.CompletedTask;
        }
    }
}