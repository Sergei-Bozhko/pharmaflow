using System.Globalization;
using System.Text.Json;

using PharmaFlow.Domain.Audit;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Tests.Common;
using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.Audit;

public class StudyAuditTrailTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset FrozenInstant =
        DateTimeOffset.Parse("2026-05-10T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Create_Study_writes_AuditEvent_row_in_same_txAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        var study = StudyBuilder.Create(clock);

        await using var ctx = CreateContext(clock);
        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditEvent = ctx.AuditEvents
            .Single(a => a.TargetEntityId == study.Id.ToString());

        Assert.Equal(AuditEventType.Create, auditEvent.EventType);
        Assert.Equal("Study", auditEvent.TargetEntityType);
        Assert.Null(auditEvent.BeforeStateJson);
        Assert.NotNull(auditEvent.AfterStateJson);
        Assert.Equal(FrozenInstant, auditEvent.OccurredAt);
        Assert.Equal(UserId.System, auditEvent.ActorUserId);
        Assert.Equal("system", auditEvent.ActorRoleAtTime);
        Assert.Equal(new string('0', 64), auditEvent.EventPayloadHash);

        using var doc = JsonDocument.Parse(auditEvent.AfterStateJson!);
        var root = doc.RootElement;
        Assert.Equal("TestProtocol", root.GetProperty("protocolNumber").GetString());
        Assert.Equal("testTitle", root.GetProperty("title").GetString());
        Assert.Equal((int)StudyPhase.PhaseI, root.GetProperty("phase").GetInt32());
    }

    [Fact]
    public async Task Update_Study_writes_AuditEvent_with_before_and_after_JSONAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        var study = StudyBuilder.Create(clock);

        await using var ctx = CreateContext(clock);
        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transition = study.SubmitForApproval();
        Assert.True(transition.IsSuccess, transition.Error?.Message);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var rows = ctx.AuditEvents
            .Where(a => a.TargetEntityId == study.Id.ToString())
            .OrderBy(a => a.OccurredAt)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(AuditEventType.Create, rows[0].EventType);
        Assert.Equal(AuditEventType.Update, rows[1].EventType);

        var update = rows[1];
        Assert.NotNull(update.BeforeStateJson);
        Assert.NotNull(update.AfterStateJson);

        using var beforeDoc = JsonDocument.Parse(update.BeforeStateJson!);
        using var afterDoc = JsonDocument.Parse(update.AfterStateJson!);
        Assert.Equal(
            (int)StudyStatus.Draft,
            beforeDoc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            (int)StudyStatus.PendingApproval,
            afterDoc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task SoftDelete_Study_writes_AuditEvent_with_SoftDelete_typeAsync()
    {
        var clock = new FrozenClock(FrozenInstant);
        var study = StudyBuilder.Create(clock);

        await using var ctx = CreateContext(clock);
        ctx.Studies.Add(study);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        ctx.Entry(study).Property("IsDeleted").CurrentValue = true;
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var softDelete = ctx.AuditEvents
            .Single(a => a.TargetEntityId == study.Id.ToString()
                    && a.EventType == AuditEventType.SoftDelete);

        Assert.NotNull(softDelete.BeforeStateJson);
        Assert.Null(softDelete.AfterStateJson);
    }
}