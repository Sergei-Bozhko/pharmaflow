using Microsoft.EntityFrameworkCore;

using Npgsql;

using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Tests.Integration.Fixtures;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "Integration")]
public abstract class IntegrationTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    protected PostgresFixture Fixture { get; } = fixture;

    public async ValueTask InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(Fixture.ConnectionString);
        await conn.OpenAsync();
        await Fixture.Respawner.ResetAsync(conn);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

}