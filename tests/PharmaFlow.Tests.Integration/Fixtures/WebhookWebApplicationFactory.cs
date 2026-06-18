using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using PharmaFlow.Infrastructure.Outbox;

namespace PharmaFlow.Tests.Integration.Fixtures;

// PFL-066: like PharmaFlowWebApplicationFactory, but loops the outbox's HTTP dispatcher back
// into THIS in-memory test server. Production's typed HttpClient opens a real socket to
// Outbox:ConsumerBaseUrl; here we override its primary handler with the test server's, so a
// flag=Http dispatch actually reaches the webhook under test (no real port). The transport
// flag itself is flipped at runtime via the OutboxOptions singleton — that's the rollback lever.
public sealed class WebhookWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);

        builder.ConfigureServices(services =>
        {
            // Drop the background poller — tests drive ProcessBatchAsync directly (PFL-060/062).
            var hostedProcessor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(OutboxProcessorService));
            if (hostedProcessor is not null)
            {
                services.Remove(hostedProcessor);
            }

            // Route the HTTP transport's typed client at the in-memory server instead of a socket.
            services.AddHttpClient<HttpIntegrationEventDispatcher>(c => c.BaseAddress = new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
        });
    }
}