using System.Globalization;
using System.Security.Claims;
using Erp.BuildingBlocks.Web.Security;
using Erp.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Erp.Api.Authentication;

/// <summary>
/// Builds the signed-in principal: identity, tenancy, and the flattened set of
/// permissions granted through the user's roles.
/// <para>
/// Flattening at sign-in means an authorization check is a claim lookup rather than
/// a database round trip per request. The trade-off is that a permission change
/// takes effect at the user's next sign-in, which is why revocation goes through
/// session invalidation rather than waiting for the cookie to expire.
/// </para>
/// </summary>
public sealed class ErpClaimsPrincipalFactory(
    UserManager<ErpUser> userManager,
    RoleManager<ErpRole> roleManager,
    IPermissionCatalogue permissionCatalogue,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ErpUser, ErpRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ErpUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(ErpClaimTypes.UserId, user.Id.ToString()));
        identity.AddClaim(new Claim(ErpClaimTypes.UserName, user.DisplayName));
        identity.AddClaim(new Claim(
            ErpClaimTypes.BusinessUnit,
            user.BusinessUnitId.ToString(CultureInfo.InvariantCulture)));

        if (user.CanAccessAllBusinessUnits)
        {
            identity.AddClaim(new Claim(ErpClaimTypes.AllBusinessUnits, "true"));
        }

        var roleNames = await UserManager.GetRolesAsync(user);
        var granted = new HashSet<string>(StringComparer.Ordinal);
        var isSuperAdministrator = false;

        foreach (var roleName in roleNames)
        {
            var role = await RoleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                continue;
            }

            if (role.IsSuperAdministrator)
            {
                isSuperAdministrator = true;
            }

            foreach (var claim in await RoleManager.GetClaimsAsync(role))
            {
                if (string.Equals(claim.Type, ErpClaimTypes.Permission, StringComparison.Ordinal))
                {
                    granted.Add(claim.Value);
                }
            }
        }

        if (isSuperAdministrator)
        {
            // Expanded from the catalogue at sign-in rather than stored, so the
            // account gains a new module's permissions the first time it signs in
            // after that module ships — with no migration and nothing to remember.
            identity.AddClaim(new Claim(ErpClaimTypes.SuperAdministrator, "true"));

            foreach (var permission in permissionCatalogue.All)
            {
                granted.Add(permission.Code);
            }
        }

        foreach (var permission in granted)
        {
            identity.AddClaim(new Claim(ErpClaimTypes.Permission, permission));
        }

        return identity;
    }
}
