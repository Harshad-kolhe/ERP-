using Erp.Contracts.Security;

namespace Erp.BuildingBlocks.Web.Security;

/// <summary>
/// Implemented once per module to publish the permissions it defines.
/// <para>
/// Discovered by assembly scan, so a new module's permissions appear on the roles
/// screen the moment it ships — nobody has to remember to add them to a central
/// list. The legacy system kept its permission rows only in the production
/// database, which meant the set of permissions that existed could not be reviewed,
/// diffed, or recreated.
/// </para>
/// </summary>
public interface IPermissionSource
{
    /// <summary>Module name, used to group the roles screen.</summary>
    string Module { get; }

    IReadOnlyList<PermissionDefinition> Permissions { get; }
}
