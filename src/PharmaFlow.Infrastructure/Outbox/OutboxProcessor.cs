using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Events;
using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    AppDbContext db,
    IDomainEventDispatcher dispatcher,
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
                var eventType = OutboxSerialization.Resolve(message.Type);
                var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(
                    message.Payload, eventType, OutboxSerialization.Options)!;
                await dispatcher.DispatchAsync(domainEvent, cancellationToken);
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