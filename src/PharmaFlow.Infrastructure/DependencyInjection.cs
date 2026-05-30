using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Time;

namespace PharmaFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPharmaFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        // PFL-050+: AppDbContext registration + interceptor wiring
        return services;
    }
}