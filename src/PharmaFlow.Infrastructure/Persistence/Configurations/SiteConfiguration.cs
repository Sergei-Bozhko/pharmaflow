using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.StudyId).IsRequired();
        b.Property(x => x.SiteNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Country).HasMaxLength(2).IsFixedLength().IsRequired();
        b.Property(x => x.PrincipalInvestigatorUserId).IsRequired();
        b.Property(x => x.ActivationDate); // nullable
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Entity<TId> inherited
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.UpdatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();

        // Indexes
        b.HasIndex(x => x.StudyId);
        b.HasIndex(x => x.PrincipalInvestigatorUserId);
        b.HasIndex(x => new { x.StudyId, x.SiteNumber }).IsUnique(); // protocol-level uniqueness
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.IsDeleted).HasFilter("\"is_deleted\" = false");
    }
}