using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Application.Modules.Sites.Contracts;
using PharmaFlow.Application.Modules.Sites.Internal;

namespace PharmaFlow.Application.Modules.Sites;

public static class SitesModuleSetup
{
    public static IServiceCollection AddSitesModule(this IServiceCollection services)
    {
        services.AddScoped<ISitesModule, SitesModule>();

        return services;
    }
}