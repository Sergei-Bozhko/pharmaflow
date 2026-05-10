using Microsoft.EntityFrameworkCore;

using PharmaFlow.Tests.Integration.Common.Helpers;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Smoke;

public sealed class PostgresFixtureSmokeTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Postgres_container_starts_and_initial_migration_apllies_Async()
    {
        var clock = new FrozenClock(DateTimeOffset.Parse("2026-05-10T12:00:00Z"));
        await using var ctx = CreateContext(clock);

        var canConnect = await ctx.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        var migrations = await ctx.Database.GetAppliedMigrationsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(canConnect);
        Assert.Contains(migrations, m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Respawner_reset_leaves_schema_intact_Async()
    {
        var clock = new FrozenClock(DateTimeOffset.Parse("2026-05-10T12:00:00Z"));
        await using var ctx = CreateContext(clock);

        var studyCount = await ctx.Studies.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, studyCount);
    }
}