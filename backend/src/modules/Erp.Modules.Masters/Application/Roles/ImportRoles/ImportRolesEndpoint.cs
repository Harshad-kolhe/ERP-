using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Roles.ImportRoles;

internal sealed class ImportRolesEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MasterImportEndpointBuilder.MapImport(
            group,
            "roles",
            MastersPermissions.RoleImport,
            file => new ImportRolesCommand(file));

        MasterImportEndpointBuilder.MapTemplate(
            group,
            "roles",
            "Roles",
            MastersPermissions.RoleImport,
            RoleImportColumns.All);
    }
}
