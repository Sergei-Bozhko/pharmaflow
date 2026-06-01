using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Common.Idempotency;
using PharmaFlow.Application.Common.Persistence;
using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Participants;
using PharmaFlow.Domain.Signatures;
using PharmaFlow.Domain.Sites;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Domain.Users;
using PharmaFlow.Infrastructure.Persistence.Conventions;
using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Application.Modules.Studies.Internal;

namespace PharmaFlow.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext, ISitesDbContext, IStudiesDbContext
{
    public DbSet<Study> Studies => Set<Study>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<SignatureRecord> SignatureRecords => Set<SignatureRecord>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        StronglyTypedIdConvention.Apply(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
    }
}