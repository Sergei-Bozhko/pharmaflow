using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Modules.Studies.Internal;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Modules.Studies;

public class StudiesModuleTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task StudyExistsAsync_returns_true_for_persisted_studyAsync()
    {
        await using var ctx = NewContext();
        var study = NewStudy();
        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var module = new StudiesModule(ctx);

        var exists = await module.StudyExistsAsync(study.Id, TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task StudyExistsAsync_returns_false_for_unknown_idAsync()
    {
        await using var ctx = NewContext();
        var module = new StudiesModule(ctx);

        var exists = await module.StudyExistsAsync(
            StudyId.New(), TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetStudyByIdAsync_returns_dto_for_persisted_studyAsync()
    {
        await using var ctx = NewContext();
        var study = NewStudy();
        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var module = new StudiesModule(ctx);

        var dto = await module.GetStudyByIdAsync(study.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        Assert.Equal(study.Id.Value, dto.Id);
        Assert.Equal("PROTO-054", dto.ProtocolNumber);
        Assert.Equal(StudyStatus.Draft, dto.Status);
    }

    [Fact]
    public async Task GetStudyByIdAsync_returns_null_for_unknown_idAsync()
    {
        await using var ctx = NewContext();
        var module = new StudiesModule(ctx);

        var dto = await module.GetStudyByIdAsync(
            StudyId.New(), TestContext.Current.CancellationToken);

        Assert.Null(dto);
    }

    private static Study NewStudy() =>
        Study.Create(
            StudyId.New(),
            "PROTO-054",
            "Studies Module Test",
            StudyPhase.PhaseIII,
            "Cardiology",
            "Acme Pharma",
            50,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            Clock).Value;

    // The audit interceptor populates the CreatedBy/UpdatedBy required columns on save.
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"studies-module-{Guid.NewGuid()}")
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, new SystemCurrentUser()))
            .Options);
}