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
        // Note: AuditBehavior is placed BEFORE TransactionBehavior so that the audit-row save runs
        // AFTER the handler's tx has committed/rolled back. This lets AuditBehavior observe all
        // outcomes including commit-time failures, and lets the failure-audit row survive a handler
        // rollback (PFL-046 §64). Deviates from PFL-046 §16's "innermost" narrative; trade-off
        // documented in the ticket PR description.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}