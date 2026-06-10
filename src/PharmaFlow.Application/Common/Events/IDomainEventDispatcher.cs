using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}