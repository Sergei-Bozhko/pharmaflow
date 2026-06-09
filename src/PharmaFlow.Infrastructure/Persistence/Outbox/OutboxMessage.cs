namespace PharmaFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
}