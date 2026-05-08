using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PharmaFlow.Infrastructure.Persistence;

public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringKey = "PHARMAFLOW_DEV_CONNECTION";

    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AppDbContextDesignTimeFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration[ConnectionStringKey]
            ?? throw new InvalidOperationException(
                $"{ConnectionStringKey} not set. Set it via:\n" +
                $"  dotnet user-secrets set \"{ConnectionStringKey}\" \"<connection string>\" " +
                "--project src/PharmaFlow.Infrastructure\n" +
                "or export the env var. See README → Dev DB.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}