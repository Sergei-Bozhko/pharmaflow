using Microsoft.EntityFrameworkCore;

using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Conventions;

namespace PharmaFlow.Tests.Integration.Persistence;

public sealed class StronglyTypedIdConventionTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Guid_backed_typed_id_property_gets_strongly_typed_converter()
    {
        using var ctx = CreateContext();

        var converter = ctx.Model
            .FindEntityType(typeof(Study))!
            .FindProperty(nameof(Study.Id))!
            .GetValueConverter();

        Assert.NotNull(converter);
        Assert.IsType<StronglyTypedIdValueConverter<StudyId, Guid>>(converter);
    }

    [Fact]
    public void Long_backed_typed_id_property_gets_strongly_typed_converter()
    {
        using var ctx = CreateContext();

        var converter = ctx.Model
            .FindEntityType(typeof(AuditEvent))!
            .FindProperty(nameof(AuditEvent.Id))!
            .GetValueConverter();

        Assert.NotNull(converter);
        Assert.IsType<StronglyTypedIdValueConverter<AuditEventId, long>>(converter);
    }
}