namespace PharmaFlow.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccuredAt { get; }
}