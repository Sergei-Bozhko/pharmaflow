using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    AppDbContext db,
    IIntegrationEventDispatcher dispatcher,
    IClock clock,
    OutboxOptions options
) : IOutboxProcessor
{
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        // Poll: unprocessed, under the attempt ceiling (poison rows excluded), oldest first.
        var batch = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.Attempts < options.MaxAttempts)
            .OrderBy(m => m.OccurredOn)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            try
            {
                var contractType = OutboxSerialization.Resolve(message.Type);
                var integrationEvent = (INotification)JsonSerializer.Deserialize(
                    message.Payload, contractType, OutboxSerialization.Options)!;
                await dispatcher.DispatchAsync(integrationEvent, message.Id, cancellationToken);
                message.MarkProcessed(clock.UtcNow);
            }
            catch (Exception ex)
            {
                message.RecordFailure(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return batch.Count;
    }
}