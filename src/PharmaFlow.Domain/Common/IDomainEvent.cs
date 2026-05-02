namespace PharmaFlow.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}