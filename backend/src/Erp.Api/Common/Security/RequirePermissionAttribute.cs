using Erp.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Erp.Api.Common.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute, IPermissionDeclaration
{
    public RequirePermissionAttribute(string permission)
        : base(typeof(PermissionActionFilter))
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Permission = permission;
        Arguments = [permission];
    }

    public string Permission { get; }
}

internal sealed class PermissionActionFilter(ICurrentUser currentUser, string permission) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!currentUser.IsAuthenticated)
        {
            context.Result = Problem(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "This action requires an authenticated user.",
                ProblemTypes.Unauthorized);

            return;
        }

        if (!currentUser.HasPermission(permission))
        {
            context.Result = Problem(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                $"This action requires the '{permission}' permission.",
                ProblemTypes.Forbidden);
        }
    }

    private static ObjectResult Problem(int status, string title, string detail, string type) =>
        new(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type,
        })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
}
