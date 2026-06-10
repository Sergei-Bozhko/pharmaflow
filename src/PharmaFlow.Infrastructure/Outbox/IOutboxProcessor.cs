namespace PharmaFlow.Infrastructure.Outbox;

public interface IOutboxProcessor
{
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken);
}