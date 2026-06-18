using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Application.Modules.Sites.Internal;

public interface ISitesDbContext
{
    DbSet<Site> Sites { get; }
    DbSet<KnownStudy> KnownStudies { get; }
    DbSet<InboxMessage> InboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}