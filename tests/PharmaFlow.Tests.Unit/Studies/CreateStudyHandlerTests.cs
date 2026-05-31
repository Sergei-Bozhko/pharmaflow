using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Studies.Commands.CreateStudy;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Infrastructure.Auth;
using PharmaFlow.Infrastructure.Persistence;
using PharmaFlow.Infrastructure.Persistence.Interceptors;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Studies;

public class CreateStudyHandlerTests
{
    private static readonly FrozenClock Clock =
        new(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Valid_command_persists_study_and_returns_idAsync()
    {
        await using var ctx = NewContext();
        var handler = new CreateStudyHandler(ctx, Clock);

        var result = await handler.Handle(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotEqual(Guid.Empty, result.Value.Value);

        var study = await ctx.Studies.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(result.Value, study.Id);
        Assert.Equal("PROTO-050", study.ProtocolNumber);
    }

    [Fact]
    public async Task Invalid_command_returns_failure_and_persists_nothingAsync()
    {
        await using var ctx = NewContext();
        var handler = new CreateStudyHandler(ctx, Clock);

        // Empty protocol number → Study.Create returns a domain validation failure
        // (the handler's IsFailure branch), before anything is added to the context.
        var result = await handler.Handle(
            ValidCommand() with { ProtocolNumber = "" },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("study.protocol_number.invalid", result.Error.Code);
        Assert.Equal(0, await ctx.Studies.CountAsync(TestContext.Current.CancellationToken));
    }

    private static CreateStudyCommand ValidCommand() => new(
        ProtocolNumber: "PROTO-050",
        Title: "Phase II Oncology Study",
        Phase: StudyPhase.PhaseII,
        TherapeuticArea: "Oncology",
        SponsorOrganization: "Acme Pharma",
        PlannedEnrolment: 100,
        PlannedStartDate: new DateOnly(2026, 6, 1),
        PlannedEndDate: new DateOnly(2027, 5, 31));

    // Mirrors IntegrationTestBase.CreateContext: the audit interceptor populates the
    // CreatedBy/UpdatedBy required columns on save, so it must be wired even in-memory.
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"create-study-{Guid.NewGuid()}")
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, new SystemCurrentUser()))
            .Options);
}