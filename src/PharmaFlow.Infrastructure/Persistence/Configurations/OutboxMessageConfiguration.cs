using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Infrastructure.Persistence.Outbox;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasMaxLength(200).IsRequired();
        b.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();

        b.HasIndex(x => x.OccurredOn).HasFilter("processed_on IS NULL");
    }
}