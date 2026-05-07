using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Users;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Username).HasMaxLength(40).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.DisplayTitle).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(x => x.MfaEnrolled).IsRequired();
        b.Property(x => x.LastLoginAt);
        b.Property(x => x.FailedLoginCount).IsRequired();
        b.Property(x => x.PasswordLastChangedAt);

        // Entity<TId> inherited
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.UpdatedBy).HasMaxLength(40).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();

        // Indexes
        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.IsDeleted).HasFilter("\"is_deleted\" = false");
        b.HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
    }
}