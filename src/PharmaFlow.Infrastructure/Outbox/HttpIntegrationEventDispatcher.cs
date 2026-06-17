using System.Net.Http.Json;
using System.Text.Json;

using Mediator;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class HttpIntegrationEventDispatcher(HttpClient httpClient) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(INotification notification, Guid messageId, CancellationToken cancellationToken)
    {
        var envelope = new
        {
            messageId,
            type = OutboxSerialization.NameOf(notification),
            payload = JsonSerializer.Serialize(notification, notification.GetType(), OutboxSerialization.Options),
        };

        using var resp = await httpClient.PostAsJsonAsync("/internal/integration-events/study-created",
            envelope, cancellationToken);
        resp.EnsureSuccessStatusCode();
    }
}