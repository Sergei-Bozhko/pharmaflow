namespace PharmaFlow.Application.Legacy;

public interface IAccountStatementReadModel
{
    Task<IReadOnlyList<StatementLine>> GetAsync(
        long accountId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);
}

public sealed record StatementLine
(
    long TxnId,
    DateTimeOffset PostedAt,
    DateOnly ValueDate,
    decimal Amount,
    string TxnType,
    string? Description,
    decimal RunningBalance);