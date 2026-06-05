using PharmaFlow.Domain.Common;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Common;

public class FrozenClockTests
{
    [Fact]
    public void FrozenClock_returns_value_passed_to_ctor()
    {
        var frozen = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var clock = new FrozenClock(frozen);

        Assert.Equal(frozen, clock.UtcNow);
    }

    [Fact]
    public void FrozenClock_reads_are_idempotent()
    {
        var frozen = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var clock = new FrozenClock(frozen);

        var first = clock.UtcNow;
        var second = clock.UtcNow;
        var third = clock.UtcNow;

        Assert.Equal(first, second);
        Assert.Equal(second, third);
    }

    [Fact]
    public void FrozenClock_implements_IClock()
    {
        var clock = new FrozenClock(DateTimeOffset.UtcNow);
        Assert.IsType<IClock>(clock, exactMatch: false);
    }
}