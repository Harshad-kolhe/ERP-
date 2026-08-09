using Erp.BuildingBlocks.Application.Abstractions;
using Erp.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace Erp.BuildingBlocks.Web.Security;

/// <summary>
/// Enforces the permission declared by <see cref="PermissionMetadata"/>.
/// <para>
/// Runs on the server, on every request, for every endpoint that declares one.
/// The permission list the web app receives is for deciding which buttons to draw;
/// it is never trusted, and this filter re-checks regardless of what the client believes.
/// </para>
/// </summary>
internal sealed class PermissionEndpointFilter(ICurrentUser currentUser) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var required = context.HttpContext
            .GetEndpoint()?
            .Metadata
            .GetMetadata<PermissionMetadata>();

        if (required is not null && !currentUser.HasPermission(required.Permission))
        {
            return Results.Problem(
                title: "Forbidden",
                detail: $"This action requires the '{required.Permission}' permission.",
                statusCode: StatusCodes.Status403Forbidden,
                type: ProblemTypes.Forbidden);
        }

        return await next(context);
    }
}
