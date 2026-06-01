using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Application.Modules.Sites.Internal;

internal interface ISitesDbContext
{
    DbSet<Site> Sites { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}