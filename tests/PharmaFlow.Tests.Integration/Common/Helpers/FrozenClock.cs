using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Integration.Common.Helpers;

public sealed class FrozenClock(DateTimeOffset frozenAt) : IClock
{
    public DateTimeOffset UtcNow { get; } = frozenAt;
}