using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Participants;
using PharmaFlow.Domain.Signatures;
using PharmaFlow.Domain.Sites;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Domain.Users;

namespace PharmaFlow.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Study> Studies => Set<Study>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<SignatureRecord> SignatureRecords => Set<SignatureRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}