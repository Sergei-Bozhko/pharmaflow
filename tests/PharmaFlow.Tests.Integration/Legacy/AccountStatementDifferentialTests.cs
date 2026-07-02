using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Npgsql;

using PharmaFlow.Application.Legacy;
using PharmaFlow.Infrastructure.Legacy;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Tests.Integration.Legacy;

// PFL-067 differential test. Proves the C# port (AccountStatementReadModel) returns exactly what
// the legacy dbo.fn_account_statement function returns — row for row — for the same account/window.
//
// Runs against the local dev DB (PHARMAFLOW_DEV_CONNECTION), where the dbo schema, seed, and
// functions are already applied. It SKIPS when that connection isn't configured (e.g. CI), so this
// is a local equivalence gate, not a pipeline gate. Read-only: it never mutates the dbo data.
public sealed class AccountStatementDifferentialTests
{
    private const long Account = 101;
    private static readonly DateOnly From = new(2025, 1, 1);
    private static readonly DateOnly To = new(2025, 2, 28);

    [Fact]
    public async Task Port_matches_legacy_function_row_for_rowAsync()
    {
        var connectionString = DevConnectionString();
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(connectionString),
            "PHARMAFLOW_DEV_CONNECTION not set — skipping legacy differential test (needs the dev DB with the dbo schema applied).");

        // BEFORE: the legacy function is the source of truth.
        var expected = await ReadFromFunctionAsync(connectionString);

        // Guard the seed so a changed fixture fails loudly here, not as a baffling row diff later.
        Assert.Equal(5, expected.Count);
        Assert.DoesNotContain(expected, l => l.TxnId == 7);   // value-dated Mar 3 → outside the window
        Assert.Equal(1118.01m, expected[^1].RunningBalance);  // worked-target closing balance

        // AFTER: the C# port over the same account + window, normalised to the same row shape.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var ctx = new AppDbContext(options);
        var port = new AccountStatementReadModel(ctx);
        var actual = (await port.GetAsync(Account, From, To, CancellationToken.None))
            .Select(Line.From)
            .ToList();

        // PROOF: identical, row for row, column for column.
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].TxnId, actual[i].TxnId);
            Assert.Equal(expected[i].PostedAt, actual[i].PostedAt);
            Assert.Equal(expected[i].ValueDate, actual[i].ValueDate);
            Assert.Equal(expected[i].Amount, actual[i].Amount);
            Assert.Equal(expected[i].TxnType, actual[i].TxnType);
            Assert.Equal(expected[i].Description, actual[i].Description);
            Assert.Equal(expected[i].RunningBalance, actual[i].RunningBalance);
        }
    }

    private static async Task<List<Line>> ReadFromFunctionAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT txn_id, posted_at, value_date, amount, txn_type, description, running_balance
            FROM dbo.fn_account_statement(@account, @from, @to)
            ORDER BY value_date, txn_id;
            """;
        cmd.Parameters.AddWithValue("account", Account);
        cmd.Parameters.AddWithValue("from", From);
        cmd.Parameters.AddWithValue("to", To);

        var rows = new List<Line>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new Line(
                reader.GetInt64(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetFieldValue<DateOnly>(2),
                reader.GetDecimal(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetDecimal(6)));
        }
        return rows;
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

    // Normalised row shape so the function output and the port output compare cleanly.
    private sealed record Line(
        long TxnId,
        DateTimeOffset PostedAt,
        DateOnly ValueDate,
        decimal Amount,
        string TxnType,
        string? Description,
        decimal RunningBalance)
    {
        public static Line From(StatementLine l) => new(
            l.TxnId, l.PostedAt, l.ValueDate, l.Amount, l.TxnType, l.Description, l.RunningBalance);
    }
}