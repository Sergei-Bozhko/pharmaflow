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

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload, DateTimeOffset occurredOn)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredOn = occurredOn;
        Attempts = 0;
    }

    public void MarkProcessed(DateTimeOffset processedOn) => ProcessedOn = processedOn;

    public void RecordFailure(string error)
    {
        Attempts++;
        Error = error;
    }
}