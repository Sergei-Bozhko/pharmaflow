namespace PharmaFlow.Application.Legacy;

public interface IMonthlyFeeReadModel
{
    Task<FeeAssessment> GetAsync(long accountId, DateOnly periodStart, CancellationToken ct);
}

public record FeeAssessment
(
    decimal AvgDailyBalance,
    bool FeeWaived,
    decimal MonthlyFee,
    int BillableTxns,
    int FreeTxns,
    decimal ExcessTxnFee,
    decimal TotalFee
);