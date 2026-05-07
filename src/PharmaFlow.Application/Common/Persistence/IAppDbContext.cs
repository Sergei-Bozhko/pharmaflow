using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Participants;
using PharmaFlow.Domain.Signatures;
using PharmaFlow.Domain.Sites;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Domain.Users;

namespace PharmaFlow.Application.Common.Persistence;

public interface IAppDbContext
{
    DbSet<Study> Studies { get; }
    DbSet<Site> Sites { get; }
    DbSet<Participant> Participants { get; }
    DbSet<User> Users { get; }
    DbSet<RoleAssignment> RoleAssignments { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    DbSet<SignatureRecord> SignatureRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}