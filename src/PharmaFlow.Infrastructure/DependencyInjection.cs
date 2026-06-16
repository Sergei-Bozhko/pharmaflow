using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Application.Common.Auth;
using PharmaFlow.Application.Common.Events;
using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Studies.Internal;
using PharmaFlow.Domain.Common;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Outbox;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Infrastructure.Time;

namespace PharmaFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPharmaFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<AuditingSaveChangesInterceptor>();

        services.AddSingleton(new OutboxOptions());
        services.AddScoped<IIntegrationEventDispatcher, MediatorDomainEventDispatcher>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddHostedService<OutboxProcessorService>();
        services.AddScoped<OutboxSaveChangesInterceptor>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing connection string 'ConnectionStrings:Default'."
            );

        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>())
                .AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IStudiesDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ISitesDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}