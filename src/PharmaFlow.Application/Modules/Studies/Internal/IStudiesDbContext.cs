using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Modules.Studies.Internal;

public interface IStudiesDbContext
{
    DbSet<Study> Studies { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}