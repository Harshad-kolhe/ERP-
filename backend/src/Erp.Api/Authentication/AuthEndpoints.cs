using Erp.BuildingBlocks.Application.Abstractions;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Auth;
using Erp.Contracts.Common;
using Erp.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace Erp.Api.Authentication;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", async (
                LoginRequest request,
                SignInManager<ErpUser> signInManager,
                UserManager<ErpUser> userManager) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);

                var result = user is null

                    // Same response as a wrong password, below. Returning a distinct
                    // "no such account" would turn this endpoint into a way to
                    // enumerate who works here.
                    ? SignInResult.Failed
                    : await signInManager.PasswordSignInAsync(
                        user,
                        request.Password,
                        isPersistent: false,
                        lockoutOnFailure: true);

                if (result.IsLockedOut)
                {
                    return Results.Problem(
                        title: "Account locked",
                        detail: "Too many failed attempts. Try again later.",
                        statusCode: StatusCodes.Status423Locked,
                        type: ProblemTypes.Unauthorized);
                }

                if (!result.Succeeded)
                {
                    // One message for "no such user" and "wrong password" alike, so
                    // the endpoint cannot be used to enumerate valid accounts.
                    return Results.Problem(
                        title: "Sign-in failed",
                        detail: "The email or password is incorrect.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        type: ProblemTypes.Unauthorized);
                }

                return Results.NoContent();
            })
            .WithName("Login")
            .WithSummary("Sign in")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", async (SignInManager<ErpUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.NoContent();
            })
            .WithName("Logout")
            .WithSummary("Sign out")
            .RequireAuthenticatedUserOnly()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", (ICurrentUser currentUser, IBusinessUnitContext businessUnit) =>
                Results.Ok(new CurrentUserDto
                {
                    UserId = currentUser.UserId,
                    UserName = currentUser.UserName,
                    BusinessUnitId = businessUnit.BusinessUnitId,
                    CanAccessAllBusinessUnits = businessUnit.CanAccessAllBusinessUnits,
                    Permissions = [.. currentUser.Permissions.OrderBy(p => p, StringComparer.Ordinal)],
                    IsSuperAdministrator = currentUser.IsSuperAdministrator,
                }))
            .WithName("GetCurrentUser")
            .WithSummary("Current user and permissions")
            .WithDescription(
                "The permission list drives menu and button visibility only. Every endpoint "
                + "re-checks server-side, so a client that fakes this list gains nothing.")
            .RequireAuthenticatedUserOnly()
            .Produces<CurrentUserDto>();
    }
}
