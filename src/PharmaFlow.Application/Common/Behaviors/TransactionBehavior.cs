using Mediator;

using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsCommand = ComputeIsCommand();

private readonly IAppDbContext _ctx;
    public TransactionBehavior (IAppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand)
        {
            return await next(message, cancellationToken);
        }

        var dbContext = (DbContext)_ctx;
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(message, cancellationToken);

            if (response is Result r && r.IsFailure)
            {
                await tx.RollbackAsync(cancellationToken);
                return response;
            }

            await tx.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool ComputeIsCommand()
    {
        var type = typeof(TRequest);

        if( typeof(ICommand).IsAssignableFrom(type))
        {
            return true;
        }

        return type.GetInterfaces().Any(i => 
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}