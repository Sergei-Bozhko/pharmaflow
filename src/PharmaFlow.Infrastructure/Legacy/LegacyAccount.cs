namespace PharmaFlow.Infrastructure.Legacy;

public sealed class LegacyAccount
{
    public long AccountId { get; set; }
    public long CustomerId { get; set; }
    public string AccountNo { get; set; } = default!;
    public string AccountType { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime OpenedOn { get; set; }
    public DateTime? ClosedOn { get; set; }

    private LegacyAccount() { }

    public LegacyAccount(
        long accountId,
        long customerId,
        string accountNo,
        string accountType,
        string currency,
        string status,
        DateTime openedOn,
        DateTime? closedOn
    )
    {
        AccountId = accountId;
        CustomerId = customerId;
        AccountNo = accountNo;
        AccountType = accountType;
        Currency = currency;
        Status = status;
        OpenedOn = openedOn;
        ClosedOn = closedOn;
    }
}