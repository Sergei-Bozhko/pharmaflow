using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Studies.Queries.GetStudyById;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Studies;

public class GetStudyByIdHandlerTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Existing_study_returns_mapped_dtoAsync()
    {
        await using var ctx = NewContext();
        var study = Study.Create(
            StudyId.New(),
            "PROTO-051",
            "Get Study Test",
            StudyPhase.PhaseIII,
            "Cardiology",
            "Acme Pharma",
            50,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            Clock).Value;

        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStudyByIdHandler(ctx);

        var result = await handler.Handle(
            new GetStudyByIdQuery(study.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(study.Id.Value, result.Value.Id);
        Assert.Equal("PROTO-051", result.Value.ProtocolNumber);
        Assert.Equal("Get Study Test", result.Value.Title);
        Assert.Equal(StudyPhase.PhaseIII, result.Value.Phase);
        Assert.Equal(50, result.Value.PlannedEnrolment);
        Assert.Equal(StudyStatus.Draft, result.Value.Status);
    }

    [Fact]
    public async Task Missing_study_returns_not_foundAsync()
    {
        await using var ctx = NewContext();
        var handler = new GetStudyByIdHandler(ctx);

        var result = await handler.Handle(
            new GetStudyByIdQuery(StudyId.New()), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("study.not_found", result.Error.Code);
    }

    // The audit interceptor populates the CreatedBy/UpdatedBy required columns on save.
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"get-study-{Guid.NewGuid()}")
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, new SystemCurrentUser()))
            .Options);
}