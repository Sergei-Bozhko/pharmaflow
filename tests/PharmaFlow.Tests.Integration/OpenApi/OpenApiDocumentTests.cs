using System.Net;
using System.Text.Json;

using PharmaFlow.Tests.Integration.Fixtures;

namespace PharmaFlow.Tests.Integration.OpenApi;

[Trait("Category", "Integration")]
public sealed class OpenApiDocumentTests
{
    [Fact]
    public async Task Openapi_document_is_served_with_info_titleAsync()
    {
        // OpenAPI generation never touches the database, so a throwaway
        // connection string is enough to boot the app for this test.
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new PharmaFlowWebApplicationFactory(
            "Host=localhost;Database=unused;Username=unused;Password=unused");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var title = document.RootElement
            .GetProperty("info")
            .GetProperty("title")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(title));

        // PFL-050/051: once Study endpoints land, also assert the document
        // contains "/api/v1/studies" (POST) and "/api/v1/studies/{id}" (GET).
    }
}