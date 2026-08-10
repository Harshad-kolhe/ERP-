using Erp.Api.Common.Modules;
using Erp.Api.Features.Imports;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Roles.ImportRoles;

public sealed class ImportRolesEndpoint : IEndpoint
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
