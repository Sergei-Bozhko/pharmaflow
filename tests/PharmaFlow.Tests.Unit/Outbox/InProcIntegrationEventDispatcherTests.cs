using Mediator;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Infrastructure.Outbox;

namespace PharmaFlow.Tests.Unit.Outbox;

// PFL-065 in-proc transport (rename of the old map-on-dispatch seam). Post-PFL-064 the dispatcher
// no longer maps — it receives the stored integration contract and publishes it as-is. The in-proc
// impl ignores the message id: in-proc dedup is the processor's processed_on, not a consumer inbox.
public class InProcIntegrationEventDispatcherTests
{
    private static readonly DateTimeOffset Occurred =
        new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Publishes_the_contract_onceAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var publisher = new RecordingPublisher();
        var dispatcher = new InProcIntegrationEventDispatcher(publisher);
        var studyId = StudyId.New();

        await dispatcher.DispatchAsync(
            new StudyCreatedIntegrationEvent(studyId.Value, Occurred), Guid.NewGuid(), ct);

        var published = Assert.Single(publisher.Published);
        var integrationEvent = Assert.IsType<StudyCreatedIntegrationEvent>(published);
        Assert.Equal(studyId.Value, integrationEvent.StudyId);
        Assert.Equal(Occurred, integrationEvent.OccurredAt);
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