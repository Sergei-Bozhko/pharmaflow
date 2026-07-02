namespace PharmaFlow.Infrastructure.Legacy;

public sealed class LegacyFeeSchedule
{
    public long FeeScheduleId { get; set; }
    public string AccountType { get; set; } = default!;
    public decimal MonthlyFee { get; set; }
    public decimal MinAvgBalance { get; set; }
    public int FreeTxnCount { get; set; }
    public decimal PerTxnFee { get; set; }
}