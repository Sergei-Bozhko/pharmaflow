namespace PharmaFlow.Domain.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}