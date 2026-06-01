using Microsoft.Extensions.DependencyInjection;

namespace PharmaFlow.Application.Modules.Studies;

public static class StudiesModuleSetup
{
    public static IServiceCollection AddStudiesModule(this IServiceCollection services)
    {
        // Handlers + validators auto-register via the assembly-wide scans in AddPharmaFlowApplication.
        // PFL-054 adds: services.AddScoped<IStudiesModule, StudiesModule>();
        return services;
    }
}