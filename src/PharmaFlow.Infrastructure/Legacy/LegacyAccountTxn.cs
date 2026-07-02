namespace PharmaFlow.Infrastructure.Legacy;

public sealed class LegacyAccountTxn
{
    public long TxnId { get; set; }
    public long AccountId { get; set; }
    public DateTimeOffset PostedAt { get; set; }
    public DateOnly ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string TxnType { get; set; } = default!;
    public string? Description { get; set; } = default!;

    private LegacyAccountTxn() { }

    public LegacyAccountTxn(
        int txnId,
        int accountId,
        DateTimeOffset postedAt,
        DateOnly valueDate,
        decimal amount,
        string txnType,
        string description)
    {
        TxnId = txnId;
        AccountId = accountId;
        PostedAt = postedAt;
        ValueDate = valueDate;
        Amount = amount;
        TxnType = txnType;
        Description = description;
    }

}