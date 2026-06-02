using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Application.Modules.Studies.Internal;

namespace PharmaFlow.Application.Modules.Studies;

public static class StudiesModuleSetup
{
    public static IServiceCollection AddStudiesModule(this IServiceCollection services)
    {
        // Handlers + validators auto-register via the assembly-wide scans in AddPharmaFlowApplication.
        services.AddScoped<IStudiesModule, StudiesModule>();
        return services;
    }
}