namespace PharmaFlow.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DequeueEvents();
}