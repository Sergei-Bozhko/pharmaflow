using PharmaFlow.Domain.Common;

namespace PharmaFlow.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}