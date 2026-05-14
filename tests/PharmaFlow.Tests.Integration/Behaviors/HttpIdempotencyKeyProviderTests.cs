using Microsoft.AspNetCore.Http;

using PharmaFlow.Api.Common.Idempotency;

namespace PharmaFlow.Tests.Unit.Idempotency;

public class HttpIdempotencyKeyProviderTests
{
    [Fact]
    public void GetKey_returns_null_when_no_http_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var provider = new HttpIdempotencyKeyProvider(accessor);

        Assert.Null(provider.GetKey());
    }

    [Fact]
    public void GetKey_returns_header_value_when_present()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Idempotency-Key"] = "abc-123";
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var provider = new HttpIdempotencyKeyProvider(accessor);

        Assert.Equal("abc-123", provider.GetKey());
    }

    [Fact]
    public void GetKey_returns_empty_when_header_absent()
    {
        var ctx = new DefaultHttpContext();   // no header set
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var provider = new HttpIdempotencyKeyProvider(accessor);

        Assert.Equal(string.Empty, provider.GetKey());
    }
}