using PharmaFlow.Domain.Common;

namespace PharmaFlow.Api.Common;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, HttpContext ctx) =>
        result.IsSuccess
            ? Results.NoContent()
            : Problem(result.Error, ctx);

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        HttpContext ctx,
        int successStatus = StatusCodes.Status200OK) =>
        result.IsSuccess
            ? SuccessResult(result.Value, successStatus, ctx)
            : Problem(result.Error, ctx);

    private static IResult SuccessResult<T>(T value, int status, HttpContext ctx) =>
        status switch
        {
            StatusCodes.Status201Created => Results.Created(ctx.Request.Path, value),
            StatusCodes.Status200OK => Results.Ok(value),
            _ => Results.Json(value, statusCode: status),
        };

    private static IResult Problem(Error error, HttpContext ctx)
    {
        var status = StatusFor(error.ErrorType);
        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = error.Code,
            ["traceId"] = ctx.TraceIdentifier
        };

        if (error.ErrorType == ErrorType.Validation)
        {
            extensions["errors"] = new Dictionary<string, string[]>
            {
                ["_"] = new[] { error.Message },
            };
        }

        return Results.Problem(
            detail: error.Message,
            instance: ctx.Request.Path,
            statusCode: status,
            title: error.ErrorType.ToString(),
            type: TypeUriFor(status),
            extensions: extensions);
    }

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string TypeUriFor(int status) => status switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
        403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    };
}