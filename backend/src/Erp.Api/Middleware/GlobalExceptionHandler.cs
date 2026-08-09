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
}
