using Mediator;

namespace PharmaFlow.Application.Common.Events;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(INotification notification, CancellationToken cancellationToken);
}