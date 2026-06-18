using Microsoft.EntityFrameworkCore;

using PharmaFlow.Application.Operator;
using PharmaFlow.Infrastructure.Persistence;

namespace PharmaFlow.Infrastructure.Operator;

// Reads AppDbContext directly (Infrastructure owns it) and projects flat rows for the console.
// Entities are materialised first, then mapped in memory — enum.ToString() and strongly-typed-id
// .Value don't need to translate to SQL, and the result sets are operator-sized (take-limited).
internal sealed class OperatorReadModel(AppDbContext db) : IOperatorReadModel
{
    public async Task<IReadOnlyList<StudyRow>> StudiesAsync(CancellationToken ct)
    {
        var rows = await db.Studies.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        return rows.Select(s => new StudyRow(
            s.Id.Value, s.ProtocolNumber, s.Title, s.Phase.ToString(), s.Status.ToString(),
            s.SponsorOrganization, s.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<SiteRow>> SitesAsync(Guid? studyId, CancellationToken ct)
    {
        var query = db.Sites.AsNoTracking();
        if (studyId is { } sid)
        {
            var typed = new Domain.Common.Ids.StudyId(sid);
            query = query.Where(s => s.StudyId == typed);
        }

        var rows = await query.OrderByDescending(s => s.CreatedAt).Take(200).ToListAsync(ct);

        return rows.Select(s => new SiteRow(
            s.Id.Value, s.StudyId.Value, s.SiteNumber, s.Name, s.Country, s.Status.ToString(),
            s.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<OutboxRow>> OutboxAsync(int take, CancellationToken ct)
    {
        var rows = await db.OutboxMessages.AsNoTracking()
            .OrderByDescending(m => m.OccurredOn)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(m => new OutboxRow(
            m.Id, m.Type, m.OccurredOn, m.ProcessedOn, m.Attempts, m.Error)).ToList();
    }

    public async Task<IReadOnlyList<InboxRow>> InboxAsync(int take, CancellationToken ct)
    {
        var rows = await db.InboxMessages.AsNoTracking()
            .OrderByDescending(i => i.ReceivedAt)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(i => new InboxRow(i.MessageId, i.ReceivedAt)).ToList();
    }

    public async Task<IReadOnlyList<KnownStudyRow>> KnownStudiesAsync(int take, CancellationToken ct)
    {
        var rows = await db.KnownStudies.AsNoTracking()
            .OrderByDescending(k => k.RegisteredAt)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(k => new KnownStudyRow(k.StudyId, k.RegisteredAt)).ToList();
    }

    public async Task<IReadOnlyList<AuditRow>> AuditAsync(int take, CancellationToken ct)
    {
        var rows = await db.AuditEvents.AsNoTracking()
            .OrderByDescending(a => a.Id)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(a => new AuditRow(
            a.Id.Value, a.OccurredAt, a.ActorUserId.Value, a.EventType.ToString(),
            a.TargetEntityType, a.TargetEntityId, a.ReasonForChange)).ToList();
    }
}