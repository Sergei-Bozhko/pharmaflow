using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class StudyConfiguration : IEntityTypeConfiguration<Study>
{
    public void Configure(EntityTypeBuilder<Study> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.ProtocolNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phase).HasConversion<string>().HasMaxLength(8).IsRequired();
        b.Property(x => x.TherapeuticArea).HasMaxLength(100).IsRequired();
        b.Property(x => x.SponsorOrganization).HasMaxLength(200).IsRequired();
        b.Property(x => x.PlannedEnrolment).IsRequired();
        b.Property(x => x.PlannedStartDate).IsRequired();
        b.Property(x => x.PlannedEndDate).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Entity<TId> inherited audit + concurrency columns
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.UpdatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        b.HasIndex(x => x.ProtocolNumber).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.IsDeleted).HasFilter("\"is_deleted\" = false");
        b.HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
    }
}