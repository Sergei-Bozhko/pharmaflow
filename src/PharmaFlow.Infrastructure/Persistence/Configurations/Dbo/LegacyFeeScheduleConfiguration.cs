using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Infrastructure.Legacy;

namespace PharmaFlow.Infrastructure.Persistence.Configurations.Dbo;

public sealed class LegacyFeeScheduleConfiguration : IEntityTypeConfiguration<LegacyFeeSchedule>
{
    public void Configure(EntityTypeBuilder<LegacyFeeSchedule> builder)
    {
        builder.ToTable("fee_schedule", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.FeeScheduleId);
    }
}