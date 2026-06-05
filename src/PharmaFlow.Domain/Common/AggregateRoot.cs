namespace PharmaFlow.Domain.Common;

public abstract class AggregateRoot<TId>
    : Entity<TId> where TId : struct
{
    private readonly List<IDomainEvent> _events = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;
    protected void Raise(IDomainEvent @event) => _events.Add(@event);
    public IReadOnlyList<IDomainEvent> DequeueEvents()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }
}