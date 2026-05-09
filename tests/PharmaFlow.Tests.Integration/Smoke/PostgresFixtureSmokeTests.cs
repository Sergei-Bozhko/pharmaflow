using Microsoft.EntityFrameworkCore;

using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Smoke;

public sealed class PostgresFixtureSmokeTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Postgres_container_starts_and_initial_migration_apllies_Async()
    {
        await using var ctx = CreateContext();
        
        var canConnect = await ctx.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        var migrations = await ctx.Database.GetAppliedMigrationsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(canConnect);
        Assert.Contains(migrations, m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Respawner_reset_leaves_schema_intact_Async()
    {
        await using var ctx = CreateContext();
        
        var studyCount = await ctx.Studies.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, studyCount);
    }
}