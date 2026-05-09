using Microsoft.EntityFrameworkCore;

using Npgsql;

using PharmaFlow.Infrastructure.Persistence;

using Respawn;

using Testcontainers.PostgreSql;

namespace PharmaFlow.Tests.Integration.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pharmaflow_test")
        .WithUsername("pharmaflow")
        .WithPassword("pharmaflow")
        .Build();

    public string ConnectionString => _container.GetConnectionString();
    
    public Respawner Respawner { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using (var ctx = new AppDbContext(options))
        {
            await ctx.Database.MigrateAsync();
        }

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        Respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")],
        });
    }
    
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

    [CollectionDefinition(nameof(PostgresCollection))]
    public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;