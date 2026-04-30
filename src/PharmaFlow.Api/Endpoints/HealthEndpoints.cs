namespace PharmaFlow.Api.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/health");

        group.MapGet("/live", () => Results.Ok(new { status = "alive" }))
        .AllowAnonymous();

        return group;
    }
}