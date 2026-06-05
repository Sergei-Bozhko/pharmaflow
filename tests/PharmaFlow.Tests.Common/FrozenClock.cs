using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Common;

/// <summary>An <see cref="IClock"/> stuck at a fixed instant — deterministic time for tests.</summary>
public sealed class FrozenClock(DateTimeOffset frozenAt) : IClock
{
    public DateTimeOffset UtcNow { get; } = frozenAt;
}