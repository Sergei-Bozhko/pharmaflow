using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Legacy;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Infrastructure.Legacy;

internal sealed class MonthlyFeeReadModel(AppDbContext ctx) : IMonthlyFeeReadModel
{
    public async Task<FeeAssessment> GetAsync(long accountId, DateOnly periodStart, CancellationToken ct)
    {
        var d0 = periodStart;
        var d1 = periodStart.AddMonths(1).AddDays(-1);

        var accountType = await ctx.LegacyAccounts.AsNoTracking()
                                .Where(a => a.AccountId == accountId)
                                .Select(a => a.AccountType)
                                .SingleAsync(ct);

        var sched = await ctx.LegacyFeeSchedules.AsNoTracking()
                            .Where(s => s.AccountType == accountType)
                            .SingleAsync(ct);

        var opening = await ctx.LegacyAccountTxns.AsNoTracking()
                            .Where(a => a.AccountId == accountId && a.ValueDate < d0)
                            .Select(a => (decimal?)a.Amount)
                            .SumAsync(ct) ?? 0m;

        var dailyDeltas = await ctx.LegacyAccountTxns.AsNoTracking()
                                .Where(a => a.AccountId == accountId && a.ValueDate >= d0 && a.ValueDate <= d1)
                                .GroupBy(a => a.ValueDate)
                                .Select(g => new { Day = g.Key, Delta = g.Sum(t => t.Amount) })
                                .ToListAsync(ct);

        var lookup = dailyDeltas.ToDictionary(x => x.Day, x => x.Delta);
        var running = opening;
        var sum = 0m;
        var dayCount = 0;
        for (var d = d0; d <= d1; d = d.AddDays(1))
        {
            if (lookup.TryGetValue(d, out var delta))
            {
                running += delta;
            }
            sum += running;
            dayCount++;
        }

        var adb = sum / dayCount;

        // posted_at::date BETWEEN d0 AND d1  →  half-open instant range [d0 00:00, d1+1 00:00)
        var start = new DateTimeOffset(d0.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endExclusive = new DateTimeOffset(d1.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        string[] billableTypes = ["WITHDRAWAL", "TRANSFER"];

        var txns = await ctx.LegacyAccountTxns.AsNoTracking()
                        .Where(a => a.AccountId == accountId
                            && a.PostedAt >= start
                            && a.PostedAt < endExclusive
                            && billableTypes.Contains(a.TxnType))
                        .CountAsync(ct);

        //ROUND(adb.avg_bal, 4)
        var rounded_adb = Math.Round(adb, 4, MidpointRounding.AwayFromZero);
        var fee_waived = adb >= sched.MinAvgBalance;
        var monthlyFee = fee_waived ? 0 : sched.MonthlyFee;
        //ROUND(GREATEST(0, txns.cnt - sched.free_txn_count) * sched.per_txn_fee, 2),
        var excessFee = Math.Round(Math.Max(0, txns - sched.FreeTxnCount) * sched.PerTxnFee, 2, MidpointRounding.AwayFromZero);
        //ROUND( (CASE WHEN adb.avg_bal >= sched.min_avg_balance THEN 0 ELSE sched.monthly_fee END)
        // +GREATEST(0, txns.cnt - sched.free_txn_count) * sched.per_txn_fee, 2)
        var totalFee = Math.Round(monthlyFee + excessFee, 2, MidpointRounding.AwayFromZero);

        var result = new FeeAssessment(
            rounded_adb,
            fee_waived,
            monthlyFee,
            txns,
            sched.FreeTxnCount,
            excessFee,
            totalFee);

        return result;
    }
}