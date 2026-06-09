using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Tests.Unit.Common;

public class AggregateRootTests
{
    [Fact]
    public void Raise_adds_event_to_DomainEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));
        Assert.Single(aggregate.DomainEvents);
    }

    [Fact]
    public void Raise_appends_in_order()
    {
        var aggregate = new TestAggregate();
        var first = new TestEvent(DateTimeOffset.UtcNow);
        var second = new TestEvent(DateTimeOffset.UtcNow.AddSeconds(1));

        aggregate.RaisePublic(first);
        aggregate.RaisePublic(second);

        Assert.Equal(2, aggregate.DomainEvents.Count);
        Assert.Same(first, aggregate.DomainEvents[0]);
        Assert.Same(second, aggregate.DomainEvents[1]);
    }

    [Fact]
    public void DomainEvents_is_readonly()
    {
        var aggregate = new TestAggregate();
        Assert.IsAssignableFrom<IReadOnlyList<IDomainEvent>>(aggregate.DomainEvents);
    }

    [Fact]
    public void DequeueEvents_returns_the_raised_events_in_order()
    {
        var aggregate = new TestAggregate();
        var first = new TestEvent(DateTimeOffset.UtcNow);
        var second = new TestEvent(DateTimeOffset.UtcNow.AddSeconds(1));
        aggregate.RaisePublic(first);
        aggregate.RaisePublic(second);

        var dequeued = aggregate.DequeueEvents();

        Assert.Equal(new IDomainEvent[] { first, second }, dequeued);
    }

    [Fact]
    public void DequeueEvents_clears_the_list()
    {
        var aggregate = new TestAggregate();
        aggregate.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));

        aggregate.DequeueEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DequeueEvents_is_idempotent_second_call_returns_empty()
    {
        var aggregate = new TestAggregate();
        aggregate.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));

        var first = aggregate.DequeueEvents();
        var second = aggregate.DequeueEvents();

        Assert.Single(first);
        Assert.Empty(second);
    }

    [Fact]
    public void DequeueEvents_snapshot_is_stable_when_aggregate_raises_again()
    {
        // Harvest-once: the interceptor (PFL-059) reads the returned snapshot, then the
        // aggregate may raise more events before the next save. The already-dequeued
        // snapshot must not change — i.e. DequeueEvents returns a copy, not a live view.
        var aggregate = new TestAggregate();
        aggregate.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));

        var dequeued = aggregate.DequeueEvents();
        aggregate.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));

        Assert.Single(dequeued);
    }

    private sealed class TestAggregate : AggregateRoot<StudyId>
    {
        public TestAggregate() : base() { }
        public TestAggregate(StudyId id) : base(id) { }
        public void RaisePublic(IDomainEvent e) => Raise(e);
    }

    private sealed record TestEvent(DateTimeOffset OccurredAt) : IDomainEvent;
}