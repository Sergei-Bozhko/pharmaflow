using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Idempotency;
using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Participants;
using PharmaFlow.Domain.Signatures;
using PharmaFlow.Domain.Users;

namespace PharmaFlow.Application.Common.Persistence;

public interface IAppDbContext
{
    DbSet<Participant> Participants { get; }
    DbSet<User> Users { get; }
    DbSet<RoleAssignment> RoleAssignments { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    DbSet<SignatureRecord> SignatureRecords { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}