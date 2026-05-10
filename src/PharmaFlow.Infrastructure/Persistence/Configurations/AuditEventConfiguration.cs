using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Audit;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityByDefaultColumn();

        b.Property(x => x.OccurredAt).IsRequired();
        b.Property(x => x.ActorUserId).IsRequired();
        b.Property(x => x.ActorRoleAtTime).HasMaxLength(24).IsRequired();
        b.Property(x => x.EventType).HasConversion<string>().HasMaxLength(48).IsRequired();
        b.Property(x => x.TargetEntityType).HasMaxLength(64).IsRequired();
        b.Property(x => x.TargetEntityId).HasMaxLength(64).IsRequired();
        b.Property(x => x.BeforeStateJson).HasColumnType("jsonb"); // nullable
        b.Property(x => x.AfterStateJson).HasColumnType("jsonb");
        b.Property(x => x.ReasonForChange).HasMaxLength(500);
        b.Property(x => x.SourceIpAddress).HasMaxLength(45); // IPv6 max
        b.Property(x => x.ClientInfo).HasMaxLength(500);
        b.Property(x => x.EventPayloadHash).HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.PreviousEventHash).HasMaxLength(64).IsFixedLength();

        // Indexes
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.ActorUserId);
        b.HasIndex(x => new { x.TargetEntityType, x.TargetEntityId });
        b.HasIndex(x => x.EventType);
        b.HasIndex(x => x.PreviousEventHash); // hash-chain walk
    }
}