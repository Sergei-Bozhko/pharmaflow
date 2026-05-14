using PharmaFlow.Application.Common.Idempotency;

namespace PharmaFlow.Api.Common.Idempotency;

public sealed class HttpIdempotencyKeyProvider : IIdempotencyKeyProvider
{
    private readonly IHttpContextAccessor _http;

    public HttpIdempotencyKeyProvider(IHttpContextAccessor http)
    {
        _http = http;
    }

    public string? GetKey()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;                          // no HTTP context → pass-through
        return ctx.Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
        // header missing → "" → validation failure
    }
}