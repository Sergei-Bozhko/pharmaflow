using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Modules.Studies.Internal;

internal interface IStudiesDbContext
{
    DbSet<Study> Studies { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}