using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Tests.Unit.Outbox;

public class OutboxSerializationTests
{
    public static void Every_domain_event_is_registered_for_the_outbox()
    {
        var events = typeof(IDomainEvent).Assembly.GetTypes()
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t)
                     && t is { IsInterface: false, IsAbstract: false });

        var missing = events.Where(t => !OutboxSerialization.IsRegistered(t))
                            .Select(t => t.Name)
                            .ToList();

        Assert.True(missing.Count == 0,
            $"Domain events missing from OutboxSerialization: {string.Join(", ", missing)}");
    }
}