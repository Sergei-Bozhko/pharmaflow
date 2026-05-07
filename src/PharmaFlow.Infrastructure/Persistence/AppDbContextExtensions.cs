using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Infrastructure.Persistence;

public static class AppDbContextExtensions
{
    public static async Task<Result> SaveChangesWithConcurrencyMappingAsync(
        this IAppDbContext ctx,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict(
                "concurrency",
                "Resource was modified by another request. Reload and retry."
            );
        }
    }
}