using System.Text.Json;

using Microsoft.EntityFrameworkCore.Diagnostics;

using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Infrastructure.Persistence.Interceptors;

public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var roots = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .ToList();

        foreach (var root in roots)
        {
            foreach (var evt in root.DequeueEvents())
            {
                context.Set<OutboxMessage>().Add(
                    new OutboxMessage(
                        type: OutboxSerialization.NameOf(evt),
                        payload: JsonSerializer.Serialize(evt, evt.GetType(), OutboxSerialization.Options),
                        occurredOn: evt.OccurredAt
                    )
                );
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}