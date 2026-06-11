using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using PharmaFlow.Infrastructure.Outbox;

namespace PharmaFlow.Tests.Integration.Fixtures;

public sealed class PharmaFlowWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);

        // Drop the background outbox poller in tests: the timer races assertions about
        // unprocessed/processed rows. Tests drive OutboxProcessor.ProcessBatchAsync directly
        // for determinism (PFL-060/062 — "don't test the timer").
        builder.ConfigureServices(services =>
        {
            var hostedProcessor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(OutboxProcessorService));
            if (hostedProcessor is not null)
            {
                services.Remove(hostedProcessor);
            }
        });
    }
}