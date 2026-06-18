using PharmaFlow.Application.Operator;
using PharmaFlow.Infrastructure.Outbox;

namespace PharmaFlow.Api.Endpoints.Ops;

// Operator-console surface: read-only projections over the system + the transport flag control.
// Reads go through IOperatorReadModel (no EF in endpoints); the transport switch mutates the
// OutboxOptions singleton the running processor reads per-dispatch — the live rollback lever.
public static class OpsEndpoints
{
    public static IEndpointRouteBuilder MapOps(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/ops").WithTags("Operator");

        group.MapGet("studies", (IOperatorReadModel read, CancellationToken ct) =>
            read.StudiesAsync(ct));

        group.MapGet("sites", (IOperatorReadModel read, Guid? studyId, CancellationToken ct) =>
            read.SitesAsync(studyId, ct));

        group.MapGet("outbox", (IOperatorReadModel read, int? take, CancellationToken ct) =>
            read.OutboxAsync(take ?? 50, ct));

        group.MapGet("inbox", (IOperatorReadModel read, int? take, CancellationToken ct) =>
            read.InboxAsync(take ?? 50, ct));

        group.MapGet("known-studies", (IOperatorReadModel read, int? take, CancellationToken ct) =>
            read.KnownStudiesAsync(take ?? 50, ct));

        group.MapGet("audit", (IOperatorReadModel read, int? take, CancellationToken ct) =>
            read.AuditAsync(take ?? 50, ct));

        group.MapGet("transport", (OutboxOptions options) =>
            Results.Ok(new TransportState(options.Transport.ToString())));

        group.MapPut("transport", (SetTransportRequest body, OutboxOptions options) =>
        {
            if (!Enum.TryParse<OutboxOptions.IntegrationTransport>(body.Transport, ignoreCase: true, out var transport))
            {
                return Results.BadRequest($"Unknown transport '{body.Transport}'. Use InProc or Http.");
            }

            options.Transport = transport;
            return Results.Ok(new TransportState(options.Transport.ToString()));
        });

        return app;
    }

    private sealed record TransportState(string Transport);

    private sealed record SetTransportRequest(string Transport);
}