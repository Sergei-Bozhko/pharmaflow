using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Legacy;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Infrastructure.Legacy;

internal sealed class AccountStatementReadModel(AppDbContext ctx) : IAccountStatementReadModel
{
    public async Task<IReadOnlyList<StatementLine>> GetAsync(
        long accountId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var rows = await ctx.LegacyAccountTxns.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.ValueDate <= to)
            .OrderBy(x => x.ValueDate).ThenBy(x => x.TxnId)
            .ToListAsync(ct);

        var lines = new List<StatementLine>();
        decimal running = 0m;

        foreach (var t in rows)
        {
            running += t.Amount;
            if (t.ValueDate >= from)
                lines.Add(new StatementLine(
                    t.TxnId,
                    t.PostedAt,
                    t.ValueDate,
                    t.Amount,
                    t.TxnType,
                    t.Description,
                    running
                ));
        }
        return lines;
    }
}