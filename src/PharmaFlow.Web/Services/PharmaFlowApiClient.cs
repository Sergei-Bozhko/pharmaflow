using System.Net.Http.Json;

namespace PharmaFlow.Web.Services;

// Thin typed HttpClient over the PharmaFlow Api. Auto-generates an Idempotency-Key per create so
// the operator never crafts one. Create methods return null on success, or an error string to show.
public sealed class PharmaFlowApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<StudyRow>> StudiesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<StudyRow>>("api/v1/ops/studies", ct) ?? [];

    public async Task<IReadOnlyList<SiteRow>> SitesAsync(Guid? studyId = null, CancellationToken ct = default)
    {
        var url = studyId is { } id ? $"api/v1/ops/sites?studyId={id}" : "api/v1/ops/sites";
        return await http.GetFromJsonAsync<List<SiteRow>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<OutboxRow>> OutboxAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<OutboxRow>>("api/v1/ops/outbox", ct) ?? [];

    public async Task<IReadOnlyList<InboxRow>> InboxAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<InboxRow>>("api/v1/ops/inbox", ct) ?? [];

    public async Task<IReadOnlyList<KnownStudyRow>> KnownStudiesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<KnownStudyRow>>("api/v1/ops/known-studies", ct) ?? [];

    public async Task<IReadOnlyList<AuditRow>> AuditAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<AuditRow>>("api/v1/ops/audit", ct) ?? [];

    public async Task<string> TransportAsync(CancellationToken ct = default) =>
        (await http.GetFromJsonAsync<TransportState>("api/v1/ops/transport", ct))?.Transport ?? "Unknown";

    public async Task<string> SetTransportAsync(string transport, CancellationToken ct = default)
    {
        using var resp = await http.PutAsJsonAsync("api/v1/ops/transport", new { transport }, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TransportState>(ct))?.Transport ?? "Unknown";
    }

    public Task<string?> CreateStudyAsync(CreateStudyRequest req, CancellationToken ct = default) =>
        PostWithIdempotencyAsync("api/v1/studies", req, ct);

    public Task<string?> CreateSiteAsync(CreateSiteRequest req, CancellationToken ct = default) =>
        PostWithIdempotencyAsync("api/v1/sites", req, ct);

    private async Task<string?> PostWithIdempotencyAsync<T>(string url, T body, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        msg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using var resp = await http.SendAsync(msg, ct);
        if (resp.IsSuccessStatusCode)
        {
            return null;
        }

        var detail = await resp.Content.ReadAsStringAsync(ct);
        return $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {detail}";
    }
}