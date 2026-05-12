using FluentValidation;

using Mediator;

using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Application.Common.Behaviors;

namespace PharmaFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPharmaFlowApplication(this IServiceCollection services)
    {
        services.AddMediator(opts => opts.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Spec §9.2 order — outer → inner. DO NOT REORDER without updating BehaviorRegistrationOrderTests.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));      // PFL-042
        // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));   // PFL-043
        // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));  // PFL-045
        // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));  // PFL-044
        // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));        // PFL-046

        return services;
    }
}