using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PharmaFlow.Tests.Integration.Fixtures;

public sealed class PharmaFlowWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        // base.ConfigureWebHost(builder);
    }
}