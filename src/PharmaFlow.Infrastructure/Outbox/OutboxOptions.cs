namespace PharmaFlow.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; init; } = 20;
    public int MaxAttempts { get; init; } = 5;

    public enum IntegrationTransport { InProc, Http }
    public IntegrationTransport Transport { get; set; } = IntegrationTransport.InProc;
}