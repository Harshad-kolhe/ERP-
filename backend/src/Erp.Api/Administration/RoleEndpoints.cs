using System.Security.Claims;
using Erp.Api.Authentication;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.BuildingBlocks.Web.Security;
using Erp.Contracts.Common;
using Erp.Contracts.Security;
using Erp.Persistence;
using Erp.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Administration;

/// <summary>
/// The screen that makes permissions grantable.
/// <para>
/// This is where the role-to-permission mapping is made, and the only place it is
/// made. No source file states which permissions a role holds; this endpoint writes
/// that decision into the database, where an administrator can change it without a
/// deployment.
/// </para>
/// <para>
/// Mapped by the host rather than by a module because Identity — <c>ErpRole</c>,
/// <c>RoleManager</c> — currently lives here. It moves to <c>Erp.Modules.Identity</c>
/// with the rest of Identity; the route and contract stay the same when it does.
/// </para>
/// </summary>
internal static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin").WithTags("Roles");

        // --- The catalogue the permission picker binds to ----------------------
        group.MapGet("/permissions", (IPermissionCatalogue catalogue) =>
                Results.Ok(new PagedResult<PermissionDefinition>(
                    catalogue.All,
                    page: 1,
                    pageSize: catalogue.All.Count,
                    totalCount: catalogue.All.Count)))
            .WithName("ListPermissions")
            .WithSummary("Every permission the system defines")
            .WithDescription(
                "What can be granted. Assembled from each module's IPermissionSource at startup, "
                + "so a new module's permissions appear here without anyone maintaining a list.")
            .RequirePermission(AdminPermissions.RoleRead)
            .Produces<PagedResult<PermissionDefinition>>();

        // --- Roles -------------------------------------------------------------
        group.MapGet("/roles", async (
                int? page,
                int? pageSize,
                ErpDbContext db,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort: null, search: null, filter: null);

                var query = db.Roles
                    .AsNoTracking()
                    .OrderBy(role => role.Name)
                    .Select(role => new RoleListItemDto
                    {
                        Id = role.Id,
                        Name = role.Name!,
                        Description = role.Description,
                        PermissionCount = db.RoleClaims.Count(claim =>
                            claim.RoleId == role.Id && claim.ClaimType == ErpClaimTypes.Permission),
                        UserCount = db.UserRoles.Count(link => link.RoleId == role.Id),
                        IsSuperAdministrator = role.IsSuperAdministrator,
                    });

                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);

                return Results.Ok(new PagedResult<RoleListItemDto>(items, request.Page, request.PageSize, total));
            })
            .WithName("ListRoles")
            .WithSummary("List roles")
            .RequirePermission(AdminPermissions.RoleRead)
            .Produces<PagedResult<RoleListItemDto>>();

        group.MapGet("/roles/{id:guid}", async (
                Guid id,
                ErpDbContext db,
                CancellationToken cancellationToken) =>
            {
                var role = await db.Roles
                    .AsNoTracking()
                    .Where(candidate => candidate.Id == id)
                    .Select(candidate => new
                    {
                        candidate.Id,
                        candidate.Name,
                        candidate.Description,
                        UserCount = db.UserRoles.Count(link => link.RoleId == candidate.Id),
                        candidate.IsSuperAdministrator,
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (role is null)
                {
                    return NotFound(id);
                }

                var permissions = await db.RoleClaims
                    .AsNoTracking()
                    .Where(claim => claim.RoleId == id && claim.ClaimType == ErpClaimTypes.Permission)
                    .Select(claim => claim.ClaimValue!)
                    .OrderBy(code => code)
                    .ToListAsync(cancellationToken);

                return Results.Ok(new RoleDetailDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    Permissions = permissions,
                    UserCount = role.UserCount,
                    IsSuperAdministrator = role.IsSuperAdministrator,
                });
            })
            .WithName("GetRoleById")
            .WithSummary("Get a role and its permissions")
            .RequirePermission(AdminPermissions.RoleRead)
            .Produces<RoleDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/roles", async (
                CreateRoleRequest request,
                RoleManager<ErpRole> roleManager) =>
            {
                if (await roleManager.FindByNameAsync(request.Name) is not null)
                {
                    return Conflict($"A role named '{request.Name}' already exists.");
                }

                var role = new ErpRole(request.Name) { Description = request.Description ?? string.Empty };
                var created = await roleManager.CreateAsync(role);

                if (!created.Succeeded)
                {
                    return IdentityFailure(created);
                }

                await SyncPermissionsAsync(roleManager, role, request.Permissions);

                return Results.Created($"/api/v1/admin/roles/{role.Id}", new { id = role.Id });
            })
            .WithName("CreateRole")
            .WithSummary("Create a role")
            .RequirePermission(AdminPermissions.RoleCreate)
            .WithValidation<CreateRoleRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/roles/{id:guid}", async (
                Guid id,
                UpdateRoleRequest request,
                RoleManager<ErpRole> roleManager) =>
            {
                var role = await roleManager.FindByIdAsync(id.ToString());

                if (role is null)
                {
                    return NotFound(id);
                }

                var duplicate = await roleManager.FindByNameAsync(request.Name);

                if (duplicate is not null && duplicate.Id != id)
                {
                    return Conflict($"A role named '{request.Name}' already exists.");
                }

                role.Name = request.Name;
                role.Description = request.Description ?? string.Empty;

                var updated = await roleManager.UpdateAsync(role);

                if (!updated.Succeeded)
                {
                    return IdentityFailure(updated);
                }

                // A super-administrator role grants from the catalogue, not from stored
                // rows. Writing the picker's selection onto it would leave permission
                // rows that look authoritative and are not.
                if (!role.IsSuperAdministrator)
                {
                    await SyncPermissionsAsync(roleManager, role, request.Permissions);
                }

                return Results.NoContent();
            })
            .WithName("UpdateRole")
            .WithSummary("Update a role and its permissions")
            .WithDescription(
                "Permissions are replaced wholesale with the set supplied. Users holding this role "
                + "pick up the change at their next sign-in, because permissions are flattened onto "
                + "the principal when the session is issued.")
            .RequirePermission(AdminPermissions.RoleUpdate)
            .WithValidation<UpdateRoleRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Brings the role's permission claims in line with the requested set.
    /// <para>
    /// Computed as a diff rather than delete-all-then-insert, so an unrelated failure
    /// part-way cannot leave a role holding nothing — which for an administrator role
    /// would lock everyone out of the screen needed to fix it.
    /// </para>
    /// </summary>
    private static async Task SyncPermissionsAsync(
        RoleManager<ErpRole> roleManager,
        ErpRole role,
        IReadOnlyList<string> requested)
    {
        var existing = (await roleManager.GetClaimsAsync(role))
            .Where(claim => string.Equals(claim.Type, ErpClaimTypes.Permission, StringComparison.Ordinal))
            .ToList();

        var target = requested.ToHashSet(StringComparer.Ordinal);
        var current = existing.Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);

        foreach (var claim in existing.Where(claim => !target.Contains(claim.Value)))
        {
            await roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var code in target.Where(code => !current.Contains(code)))
        {
            await roleManager.AddClaimAsync(role, new Claim(ErpClaimTypes.Permission, code));
        }
    }

    private static IResult NotFound(Guid id) => Results.Problem(
        title: "Not found",
        detail: $"No role with id '{id}' exists.",
        statusCode: StatusCodes.Status404NotFound,
        type: ProblemTypes.NotFound,
        extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = "role.not_found" });

    private static IResult Conflict(string detail) => Results.Problem(
        title: "Conflict",
        detail: detail,
        statusCode: StatusCodes.Status409Conflict,
        type: ProblemTypes.Conflict,
        extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = "role.name.duplicate" });

    private static IResult IdentityFailure(IdentityResult result) => Results.Problem(
        title: "Could not save the role",
        detail: string.Join(" ", result.Errors.Select(error => error.Description)),
        statusCode: StatusCodes.Status400BadRequest,
        type: ProblemTypes.Validation);
}
