using PharmaFlow.Application.Legacy;

namespace PharmaFlow.Tests.Unit.Legacy;

public class FeeAssessmentTests
{
    private static FeeAssessment NewSample() =>
        new(
            AvgDailyBalance: 1080.6490m,
            FeeWaived: false,
            MonthlyFee: 12m,
            BillableTxns: 2,
            FreeTxns: 5,
            ExcessTxnFee: 0m,
            TotalFee: 12.00m
        );

    [Fact]
    public void Ctor_maps_positional_fields()
    {
        var a = NewSample();

        Assert.Equal(1080.6490m, a.AvgDailyBalance);
        Assert.False(a.FeeWaived);
        Assert.Equal(12m, a.MonthlyFee);
        Assert.Equal(2, a.BillableTxns);
        Assert.Equal(5, a.FreeTxns);
        Assert.Equal(0m, a.ExcessTxnFee);
        Assert.Equal(12.00m, a.TotalFee);
    }

    [Fact]
    public void Records_with_same_values_are_equal()
    {
        var a = NewSample();
        var b = NewSample();

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Records_with_different_values_are_not_equal()
    {
        var a = NewSample();
        var b = a with { FeeWaived = true, MonthlyFee = 0m, TotalFee = 0m };

        Assert.NotEqual(a, b);
        Assert.True(a != b);
        Assert.True(b.FeeWaived);
        Assert.Equal(0m, b.MonthlyFee);
    }

    [Fact]
    public void Deconstruct_yields_all_fields()
    {
        var (adb, waived, monthlyFee, billable, free, excess, total) = NewSample();

        Assert.Equal(1080.6490m, adb);
        Assert.False(waived);
        Assert.Equal(12m, monthlyFee);
        Assert.Equal(2, billable);
        Assert.Equal(5, free);
        Assert.Equal(0m, excess);
        Assert.Equal(12.00m, total);
    }
}