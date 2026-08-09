using System.Diagnostics;
using Erp.Contracts.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Middleware;

/// <summary>
/// Last line of defence for unhandled exceptions.
/// <para>
/// The exception is logged in full, with its trace id. The <em>response</em>
/// carries only that trace id. This is the single most important difference from
/// the system it replaces, which returned <c>ex.Message</c> to the browser in
/// roughly fifty places — including raw inner-exception text on the login page,
/// which leaked database and server names to anyone who could reach it.
/// </para>
/// <para>
/// A support call now starts with "what was the reference?" and the trace id leads
/// straight to the log entry.
/// </para>
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        // A body the framework could not read is the caller's mistake, not a fault
        // here — malformed JSON, a missing required property, a payload over the
        // limit. It reached this handler because model binding throws before any
        // endpoint filter runs, so ValidationFilter never sees it.
        //
        // Left alone it was answered 500, which is wrong twice over: it tells an
        // honest client to retry something that will never succeed, and it files a
        // client error under server faults, where it competes for attention with
        // real outages.
        if (exception is BadHttpRequestException badRequest)
        {
            await WriteBadRequestAsync(httpContext, badRequest, traceId, cancellationToken);
            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path}. TraceId {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        var problem = new ProblemDetails
        {
            Type = ProblemTypes.Unexpected,
            Title = "Unexpected error",

            // Deliberately generic. Nothing about the exception reaches the client.
            Detail = "The request could not be completed. Quote the reference below when reporting this.",
            Status = StatusCodes.Status500InternalServerError,
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    /// <summary>
    /// Answers an unreadable request body.
    /// <para>
    /// Logged at Warning, not Error: somebody sending bad JSON is not an incident,
    /// and filing it as one is how an error dashboard becomes noise nobody reads.
    /// </para>
    /// <para>
    /// The detail is fixed text. <see cref="BadHttpRequestException.Message"/> names
    /// the CLR type it failed to bind — <c>Erp.Contracts.Auth.LoginRequest</c> — and
    /// on the sign-in endpoint that is an unauthenticated caller learning the shape
    /// of the internals. The same reasoning as the 500 path above: the log gets
    /// everything, the response gets a trace id.
    /// </para>
    /// </summary>
    private async Task WriteBadRequestAsync(
        HttpContext httpContext,
        BadHttpRequestException exception,
        string traceId,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            exception,
            "Malformed request on {Method} {Path}. TraceId {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        // The framework already decided what this is worth — 400 for unreadable
        // JSON, 413 for an oversized payload. Echoing its own answer keeps the two
        // from disagreeing.
        var status = exception.StatusCode is >= 400 and < 500
            ? exception.StatusCode
            : StatusCodes.Status400BadRequest;

        var problem = new ProblemDetails
        {
            Type = ProblemTypes.Validation,
            Title = "Malformed request",
            Detail = "The request body could not be read. Check that it is valid JSON "
                + "and that every required field is present.",
            Status = status,
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}
