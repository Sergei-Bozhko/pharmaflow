using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Infrastructure.Persistence.Conventions;

/// <summary>
/// Registers <see cref="StronglyTypedIdValueConverter{TId,TKey}"/> for every strongly-typed ID
/// in the domain. EF's <c>ConfigureConventions</c> operates at the property *type* level,
/// so this catches both PKs (e.g. <c>Study.Id : StudyId</c>) and FKs (e.g. <c>Site.StudyId : StudyId</c>)
/// automatically — no per-aggregate plumbing needed.
///
/// Pattern adapted from the EF Core docs "Pre-convention model configuration":
/// https://learn.microsoft.com/en-us/ef/core/modeling/bulk-configuration#pre-convention-configuration
///
/// New typed IDs (e.g. <c>DocumentId</c> Sprint 7, <c>ConsentId</c> Sprint 9) are added by
/// appending one line below. Explicit over reflection: greppable, refactor-safe, AOT-friendly,
/// reviewable.
/// </summary>
internal static class StronglyTypedIdConvention
{
    public static void Apply(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<StudyId>()
            .HaveConversion<StronglyTypedIdValueConverter<StudyId, Guid>>();
        configurationBuilder.Properties<SiteId>()
            .HaveConversion<StronglyTypedIdValueConverter<SiteId, Guid>>();
        configurationBuilder.Properties<ParticipantId>()
            .HaveConversion<StronglyTypedIdValueConverter<ParticipantId, Guid>>();
        configurationBuilder.Properties<UserId>()
            .HaveConversion<StronglyTypedIdValueConverter<UserId, Guid>>();
        configurationBuilder.Properties<RoleAssignmentId>()
            .HaveConversion<StronglyTypedIdValueConverter<RoleAssignmentId, Guid>>();
        configurationBuilder.Properties<SignatureId>()
            .HaveConversion<StronglyTypedIdValueConverter<SignatureId, Guid>>();
        configurationBuilder.Properties<AuditEventId>()
            .HaveConversion<StronglyTypedIdValueConverter<AuditEventId, long>>();
    }
}