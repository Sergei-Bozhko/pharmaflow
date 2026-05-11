using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PharmaFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPharmaFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PFL-049: IClock → SystemClock
        // PFL-050+: AppDbContext registration + interceptor wiring
        return services;
    }
}