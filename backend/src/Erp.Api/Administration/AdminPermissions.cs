using Erp.BuildingBlocks.Web.Security;
using Erp.Contracts.Security;

namespace Erp.Api.Administration;

/// <summary>
/// Permissions for administering the security model itself.
/// <para>
/// Deliberately separate from <c>masters.role.*</c>, which belongs to the Masters
/// module's legacy Role reference table. Two different things are called "role" in
/// this system and only one of them grants anything:
/// </para>
/// <list type="bullet">
///   <item><c>masters.Role</c> — a master record carried over from the legacy schema
///   (RolesName, IsActive, BypassBusinessUnit). Reference data. Grants nothing.</item>
///   <item>Identity role — what these permissions administer. Holds permission
///   claims, and is the only thing that grants anything.</item>
/// </list>
/// <para>
/// Granting someone <c>admin.role.update</c> lets them grant themselves anything
/// else, so it is the most sensitive permission in the system and is kept apart
/// from ordinary master-data maintenance for exactly that reason.
/// </para>
/// </summary>
public static class AdminPermissions
{
    public const string RoleRead = "admin.role.read";

    public const string RoleCreate = "admin.role.create";

    /// <summary>Effectively grants everything: a holder can add any permission to their own role.</summary>
    public const string RoleUpdate = "admin.role.update";
}

/// <summary>Publishes the host's own permissions into the catalogue.</summary>
public sealed class AdminPermissionSource : IPermissionSource
{
    public string Module => "Administration";

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new(AdminPermissions.RoleRead, "View roles and permissions", "Roles", "Administration"),
        new(AdminPermissions.RoleCreate, "Create roles", "Roles", "Administration"),
        new(AdminPermissions.RoleUpdate, "Edit roles and grant permissions", "Roles", "Administration"),
    ];
}
