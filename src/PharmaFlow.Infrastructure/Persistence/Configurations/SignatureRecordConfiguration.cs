using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Domain.Signatures;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class SignatureRecordConfiguration : IEntityTypeConfiguration<SignatureRecord>
{
    public void Configure(EntityTypeBuilder<SignatureRecord> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.SignerUserId).IsRequired();
        b.Property(x => x.SignedAt).IsRequired();
        b.Property(x => x.Meaning).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.TargetEntityType).HasMaxLength(64).IsRequired();
        b.Property(x => x.TargetEntityId).HasMaxLength(64).IsRequired();
        b.Property(x => x.TargetVersionOrHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ReasonStatement).HasMaxLength(500).IsRequired();
        b.Property(x => x.AuthenticationMethod).HasConversion<string>().HasMaxLength(24).IsRequired();
        b.Property(x => x.SignaturePayloadHash).HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.PreviousSignatureHash).HasMaxLength(64).IsFixedLength();
        b.Property(x => x.ClientIp).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.MfaMethod).HasMaxLength(24);
        b.Property(x => x.ContinuousSession).IsRequired();
        b.Property(x => x.CorrelationId).HasMaxLength(64);
        b.Property(x => x.SigningKeyId).HasMaxLength(64);

        // Indexes
        b.HasIndex(x => x.SignerUserId);
        b.HasIndex(x => new { x.TargetEntityType, x.TargetEntityId });
        b.HasIndex(x => x.SignedAt);
        b.HasIndex(x => x.Meaning);
        b.HasIndex(x => x.PreviousSignatureHash); // hash-chain walk
    }
}