using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Participants;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.SiteId).IsRequired();
        b.Property(x => x.SubjectNumber).HasMaxLength(9).IsRequired(); // S-XXX-XXX = 9 chars; pad for safety
        b.Property(x => x.Initials).HasMaxLength(3); // nullable
        b.Property(x => x.YearOfBirth).IsRequired();
        b.Property(x => x.Sex).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(x => x.EnrolmentStatus).HasConversion<string>().HasMaxLength(24).IsRequired();
        b.Property(x => x.ScreeningDate); // nullable
        b.Property(x => x.EnrolmentDate);
        b.Property(x => x.WithdrawalDate);
        b.Property(x => x.WithdrawalReason).HasMaxLength(500);

        // Entity<TId> inherited audit + concurrency columns
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.UpdatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        b.HasIndex(x => x.SiteId);
        b.HasIndex(x => new { x.SiteId, x.SubjectNumber }).IsUnique(); // subject-number uniqueness within a site
        b.HasIndex(x => x.EnrolmentStatus);
        b.HasIndex(x => x.IsDeleted).HasFilter("\"is_deleted\" = false");
        b.HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
    }
}