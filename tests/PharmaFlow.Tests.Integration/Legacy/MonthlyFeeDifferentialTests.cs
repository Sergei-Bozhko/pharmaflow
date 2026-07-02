using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Npgsql;

using PharmaFlow.Application.Legacy;
using PharmaFlow.Infrastructure.Legacy;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Tests.Integration.Legacy;

// PFL-068 differential test. Proves the C# port (MonthlyFeeReadModel) returns exactly what the
// legacy dbo.fn_assess_monthly_fees function returns — column for column — for the same account/month.
//
// Runs against the local dev DB (PHARMAFLOW_DEV_CONNECTION), where the dbo schema, seed, and
// functions are already applied. It SKIPS when that connection isn't configured (e.g. CI), so this
// is a local equivalence gate, not a pipeline gate. Read-only: it never mutates the dbo data.
//
// Coverage gap logged deliberately: the seed has no month where billable debits exceed
// free_txn_count, so only the FLOOR-at-zero branch of the excess-txn fee is exercised — a
// *positive* excess fee is not differentially covered here.
public sealed class MonthlyFeeDifferentialTests
{
    // Worked targets over 01-seed.sql — these guard the fixture: if the seed drifts, the legacy
    // function stops matching these and the test fails loudly here, not as a baffling port diff.
    public static TheoryData<long, DateOnly, FeeAssessment> Cases() => new()
    {
        // 101 CHECKING — fee not waived (ADB 1080.6490 < min_avg_balance 1500), 2 billable of 5 free.
        { 101, new DateOnly(2025, 1, 1), new FeeAssessment(1080.6490m, false, 12m, 2, 5, 0m, 12.00m) },
        // 102 SAVINGS — fee waived (ADB 17419.3548 >= min_avg_balance 0), 1 billable of 9 free.
        { 102, new DateOnly(2025, 1, 1), new FeeAssessment(17419.3548m, true, 0m, 1, 9, 0m, 0m) },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Port_matches_legacy_function_column_for_columnAsync(
        long account, DateOnly periodStart, FeeAssessment expected)
    {
        var connectionString = DevConnectionString();
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(connectionString),
            "PHARMAFLOW_DEV_CONNECTION not set — skipping legacy differential test (needs the dev DB with the dbo schema applied).");

        // BEFORE: the legacy function is the source of truth.
        var fromFunction = await ReadFromFunctionAsync(connectionString, account, periodStart);

        // Seed guard: the legacy function must still produce the worked target for this fixture.
        AssertEqual(expected, fromFunction);

        // AFTER: the C# port over the same account + month.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var ctx = new AppDbContext(options);
        var port = new MonthlyFeeReadModel(ctx);
        var fromPort = await port.GetAsync(account, periodStart, CancellationToken.None);

        // PROOF: port equals the function, column for column.
        AssertEqual(fromFunction, fromPort);
    }

    private static void AssertEqual(FeeAssessment expected, FeeAssessment actual)
    {
        Assert.Equal(expected.AvgDailyBalance, actual.AvgDailyBalance);
        Assert.Equal(expected.FeeWaived, actual.FeeWaived);
        Assert.Equal(expected.MonthlyFee, actual.MonthlyFee);
        Assert.Equal(expected.BillableTxns, actual.BillableTxns);
        Assert.Equal(expected.FreeTxns, actual.FreeTxns);
        Assert.Equal(expected.ExcessTxnFee, actual.ExcessTxnFee);
        Assert.Equal(expected.TotalFee, actual.TotalFee);
    }

    private static async Task<FeeAssessment> ReadFromFunctionAsync(
        string connectionString, long account, DateOnly periodStart)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT avg_daily_balance, fee_waived, monthly_fee,
                   billable_txns, free_txns, excess_txn_fee, total_fee
            FROM dbo.fn_assess_monthly_fees(@account, @period);
            """;
        cmd.Parameters.AddWithValue("account", account);
        cmd.Parameters.AddWithValue("period", periodStart);

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), "fn_assess_monthly_fees returned no row.");

        var row = new FeeAssessment(
            reader.GetDecimal(0),
            reader.GetBoolean(1),
            reader.GetDecimal(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetDecimal(5),
            reader.GetDecimal(6));

        Assert.False(await reader.ReadAsync(), "fn_assess_monthly_fees returned more than one row.");
        return row;
    }

    private static string? DevConnectionString()
    {
        // Same resolution as AppDbContextDesignTimeFactory: Infrastructure user-secrets + env var.
        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(AppDbContext).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();
        return config["PHARMAFLOW_DEV_CONNECTION"];
    }
}