using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PharmaFlow.Application.Modules.Sites.Internal;

namespace PharmaFlow.Infrastructure.Persistence.Configurations;

public sealed class KnownStudiesConfiguration : IEntityTypeConfiguration<KnownStudy>
{
    public void Configure(EntityTypeBuilder<KnownStudy> builder)
    {
        builder.HasKey(x => x.StudyId);

        builder.Property(x => x.StudyId).ValueGeneratedNever(); // comes from the event, not the DB
    }
}