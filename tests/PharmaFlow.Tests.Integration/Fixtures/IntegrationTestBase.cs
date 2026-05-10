using Microsoft.EntityFrameworkCore;

using Npgsql;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Tests.Integration.Common.Helpers;

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

    protected AppDbContext CreateContext(IClock? clock = null, ICurrentUser? currentUser = null)
    {
        var resolvedClock = clock ?? new FrozenClock(DateTimeOffset.Now);
        var resolvedUser = currentUser ?? new SystemCurrentUser();
        var interceptor = new AuditingSaveChangesInterceptor(resolvedClock, resolvedUser);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options);
    }

}