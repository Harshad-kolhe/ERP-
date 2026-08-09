using System.Diagnostics;
using Erp.Contracts.Common;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

// Namespace is deliberately not `.Results`: that would shadow
// Microsoft.AspNetCore.Http.Results for every file under Erp.BuildingBlocks.Web.
namespace Erp.BuildingBlocks.Web.Http;

/// <summary>
/// Translates a <see cref="Result"/> into an HTTP response.
/// <para>
/// The single place where a domain outcome becomes a status code. Because every
/// endpoint funnels through here, an error is <em>always</em> a 4xx or 5xx with an
/// RFC 9457 body. The system this replaces returned
/// <c>{ Status = false, AckMsg = "Error: " + ex.Message }</c> under HTTP 200, which
/// meant load balancers, dashboards and retry logic all saw a healthy application
/// while users saw failures — and exception text leaked to the browser in about
/// fifty places.
/// </para>
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : ToProblem(result.Error);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : ToProblem(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? TypedResults.NoContent() : ToProblem(result.Error);
    }

    /// <summary>Maps an <see cref="Error"/> to a problem response.</summary>
    public static IResult ToProblem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (statusCode, problemType, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, ProblemTypes.Validation, "Invalid request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, ProblemTypes.NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, ProblemTypes.Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, ProblemTypes.Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, ProblemTypes.Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, ProblemTypes.Unexpected, "Unexpected error"),
        };

        return Results.Problem(
            title: title,
            detail: error.Description,
            statusCode: statusCode,
            type: problemType,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                // Stable, branchable identifier. Clients switch on this, never on `detail`.
                ["code"] = error.Code,
                ["traceId"] = Activity.Current?.TraceId.ToString(),
            });
    }
}
