using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Application.Common.Idempotency;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.HasKey(x => new { x.Key, x.UserId });
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("jsonb");

        builder.HasIndex(x => x.ExpiresAt);
    }
}