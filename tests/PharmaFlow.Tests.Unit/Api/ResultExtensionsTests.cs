using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using PharmaFlow.Api.Common;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Unit.Api;

public class ResultExtensionsTests
{
    private static DefaultHttpContext Ctx(string path = "/api/v1/studies", string traceId = "trace-123")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.TraceIdentifier = traceId;
        return ctx;
    }

    [Fact]
    public void Success_void_returns_NoContent()
    {
        var http = Result.Success().ToHttpResult(Ctx());
        Assert.IsType<NoContent>(http);
    }

    [Fact]
    public void Success_value_returns_Ok_with_value()
    {
        var http = Result<int>.Success(42).ToHttpResult(Ctx());
        var ok = Assert.IsType<Ok<int>>(http);
        Assert.Equal(42, ok.Value);
    }

    [Fact]
    public void ToCreatedResult_sets_Location_to_per_resource_uri()
    {
        var http = Result<int>.Success(42).ToCreatedResult(Ctx(), id => $"/api/v1/studies/{id}");
        var created = Assert.IsType<Created<int>>(http);
        Assert.Equal(42, created.Value);
        Assert.Equal("/api/v1/studies/42", created.Location);
    }

    [Fact]
    public void ToCreatedResult_failure_returns_problem_not_location()
    {
        var err = Error.Validation("v.code", "bad");
        var http = Result<int>.Failure(err).ToCreatedResult(Ctx(), id => $"/api/v1/studies/{id}");

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal("v.code", problem.ProblemDetails.Extensions["errorCode"]);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Unexpected, 500)]
    public void Failure_maps_each_ErrorType_to_status(ErrorType type, int expected)
    {
        var err = new Error("e.code", "boom", type);
        var http = Result.Failure(err).ToHttpResult(Ctx());

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(expected, problem.StatusCode);
        Assert.Equal("e.code", problem.ProblemDetails.Extensions["errorCode"]);
        Assert.Equal("trace-123", problem.ProblemDetails.Extensions["traceId"]);
        Assert.Equal("/api/v1/studies", problem.ProblemDetails.Instance);
        Assert.Equal(type.ToString(), problem.ProblemDetails.Title);
        Assert.Equal("boom", problem.ProblemDetails.Detail);
    }

    private static readonly string[] Expected = ["field bad"];

    [Fact]
    public void Validation_failure_includes_errors_dict()
    {
        var err = Error.Validation("v.code", "field bad");
        var http = Result.Failure(err).ToHttpResult(Ctx());

        var problem = Assert.IsType<ProblemHttpResult>(http);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.ProblemDetails.Extensions["errors"]);
        Assert.Equal(Expected, errors["_"]);
    }
}