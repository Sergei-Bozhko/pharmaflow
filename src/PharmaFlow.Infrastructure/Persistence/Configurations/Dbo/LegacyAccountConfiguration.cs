using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Infrastructure.Legacy;

namespace PharmaFlow.Infrastructure.Persistence.Configurations.Dbo;

public sealed class LegacyAccountConfiguration : IEntityTypeConfiguration<LegacyAccount>
{
    public void Configure(EntityTypeBuilder<LegacyAccount> builder)
    {
        builder.ToTable("account", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.AccountId);
    }
}