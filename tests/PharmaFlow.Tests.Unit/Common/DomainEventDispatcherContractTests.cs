using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Unit.Common;

public class DomainEventDispatcherContractTests
{
    [Fact]
    public async Task Dispatcher_can_dispatch_empty_collectionAsync()
    {
        IDomainEventDispatcher dispatcher = new NoOpDispatcher();
        await dispatcher.DispatchAsync([], CancellationToken.None);
    }

    [Fact]
    public async Task Dispatcher_can_dispatch_multiple_eventsAsync()
    {
        IDomainEventDispatcher dispatcher = new NoOpDispatcher();
        IDomainEvent[] events =
        {
            new TestEvent(DateTimeOffset.UtcNow),
            new TestEvent(DateTimeOffset.UtcNow.AddSeconds(1))
        };

        await dispatcher.DispatchAsync(events, CancellationToken.None);
    }

    [Fact]
    public async Task Dispatcher_accepts_cancellation_tokenAsync()
    {
        var dispatcher = new NoOpDispatcher();
        using var cts = new CancellationTokenSource();

        await dispatcher.DispatchAsync([], cts.Token);
    }

    [Fact]
    public void IDomainEvent_carries_OccurredAt()
    {
        var occurred = DateTimeOffset.UtcNow;
        var @event = new TestEvent(occurred);

        Assert.Equal(occurred, @event.OccurredAt);
    }

    private sealed class NoOpDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken) =>
                Task.CompletedTask;
    }

    private sealed record TestEvent(DateTimeOffset OccurredAt) : IDomainEvent;
}