using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Users;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(24).IsRequired();
        b.Property(x => x.AssignedAt).IsRequired();
        b.Property(x => x.EndedAt); // nullable
        b.Property(x => x.AssignedBySignatureId).IsRequired();

        // Owned: Scope discriminated-union value object
        b.OwnsOne(x => x.Scope, scope =>
        {
            scope.Property(s => s.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
            scope.Property(s => s.StudyId); // nullable; typed-ID convention applies
            scope.Property(s => s.SiteId);  // nullable; typed-ID convention applies
        });

        // Entity<TId> inherited
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.UpdatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();

        // Indexes
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Role);
        b.HasIndex(x => x.EndedAt); // active-vs-ended filtering
        b.HasIndex(x => x.IsDeleted).HasFilter("\"is_deleted\" = false");
        b.HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        // Scope_Kind / Scope_StudyId / Scope_SiteId end up as columns on role_assignments table per OwnsOne;
        // optional index on scope_kind if access pattern needs it — defer to PFL-031 query work.
    }
}