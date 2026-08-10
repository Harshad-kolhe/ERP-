using Microsoft.AspNetCore.Identity;

namespace Erp.Api.Domain.Identity;

/// <summary>
/// A role. Permissions are attached to roles as role claims, and flattened onto
/// the principal at sign-in by <see cref="ErpClaimsPrincipalFactory"/>.
/// </summary>
public sealed class ErpRole : IdentityRole<Guid>
{
    public ErpRole()
    {
    }

    public ErpRole(string roleName)
        : base(roleName)
    {
    }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Grants every permission the system defines, now and in future.
    /// <para>
    /// A flag rather than a list, because a list is a snapshot. A role granted "all
    /// 20 permissions" on the day it was created still holds exactly those twenty
    /// after a new module ships forty more â€” which is how the bootstrap account in
    /// this repository ended up unable to reach the screen that would have fixed it.
    /// </para>
    /// <para>
    /// The flag is expanded against the permission catalogue at sign-in by
    /// <see cref="ErpClaimsPrincipalFactory"/>, so nothing downstream needs to know
    /// it exists: the endpoint filter, <c>/auth/me</c> and the navigation all see an
    /// ordinary set of permission claims. And it still states no mapping in source â€”
    /// it says "everything that exists", not which permissions those are.
    /// </para>
    /// </summary>
    public bool IsSuperAdministrator { get; set; }
}
