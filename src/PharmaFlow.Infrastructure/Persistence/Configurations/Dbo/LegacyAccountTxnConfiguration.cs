using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Infrastructure.Legacy;

namespace PharmaFlow.Infrastructure.Persistence.Configurations.Dbo;

public sealed class LegacyAccountTxnConfiguration : IEntityTypeConfiguration<LegacyAccountTxn>
{
    public void Configure(EntityTypeBuilder<LegacyAccountTxn> builder)
    {
        builder.ToTable("account_txn", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.TxnId);
    }
}