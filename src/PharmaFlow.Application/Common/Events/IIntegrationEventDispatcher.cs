using Mediator;

namespace PharmaFlow.Application.Common.Events;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(INotification notification, Guid messageId, CancellationToken cancellationToken);
}