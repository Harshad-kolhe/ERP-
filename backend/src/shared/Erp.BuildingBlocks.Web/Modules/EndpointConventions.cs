using Erp.BuildingBlocks.Web.Security;
using Erp.BuildingBlocks.Web.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Erp.BuildingBlocks.Web.Modules;

/// <summary>
/// The vocabulary every endpoint is written in.
/// </summary>
public static class EndpointConventions
{
    /// <summary>
    /// Declares the permission this endpoint requires, and enforces it server-side.
    /// <para>
    /// Every endpoint must call this. <c>EndpointConventionTests</c> walks the
    /// application's real endpoint table and fails if any endpoint is missing the
    /// resulting metadata, so the check cannot be skipped by forgetting it — only
    /// by deliberately deleting a test.
    /// </para>
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return builder
            .RequireAuthorization()
            .WithMetadata(new PermissionMetadata(permission))
            .AddEndpointFilter<PermissionEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Declares that an endpoint needs a signed-in user but no specific permission
    /// — <c>/auth/me</c> and similar.
    /// <para>
    /// A distinct, explicit convention rather than simply omitting
    /// <see cref="RequirePermission"/>, so the architecture test can tell "no
    /// permission is needed here" apart from "someone forgot".
    /// </para>
    /// </summary>
    public static RouteHandlerBuilder RequireAuthenticatedUserOnly(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .RequireAuthorization()
            .WithMetadata(new AuthenticatedOnlyMetadata())
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Validates the request body with the registered FluentValidation validators
    /// before the handler runs.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
    }
}
