using Mediator;

using Microsoft.Extensions.DependencyInjection;

using PharmaFlow.Application;
using PharmaFlow.Application.Common.Behaviors;

namespace PharmaFlow.Tests.Unit.Behaviors;

public class BehaviorRegistrationOrderTests
{
    [Fact]
    public void Behaviors_register_in_spec_order()
    {
        var services = new ServiceCollection();
        services.AddPharmaFlowApplication();

        var behaviors = services
            .Where(d => d.ServiceType.IsGenericType
                     && d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();

        // PFL-041: 0 behaviors registered (all placeholders commented).
        // PFL-042: expand to [typeof(LoggingBehavior<,>)]
        // PFL-043: expand to [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)]
        // PFL-045: expand to [..., typeof(IdempotencyBehavior<,>)]
        // PFL-044: expand to [..., typeof(TransactionBehavior<,>)]
        // PFL-046: expand to [..., typeof(AuditBehavior<,>)]
        // Assert.Empty(behaviors);
        Assert.Equal(
            new[] {
                typeof(LoggingBehavior<,>), 
                typeof(ValidationBehavior<,>),
                typeof(TransactionBehavior<,>)
                },
            behaviors);
    }
}