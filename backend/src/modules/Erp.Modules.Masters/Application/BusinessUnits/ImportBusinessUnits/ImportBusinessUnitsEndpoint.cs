using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.BusinessUnits.ImportBusinessUnits;

internal sealed class ImportBusinessUnitsEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MasterImportEndpointBuilder.MapImport(
            group,
            "business-units",
            MastersPermissions.BusinessUnitImport,
            file => new ImportBusinessUnitsCommand(file));

        MasterImportEndpointBuilder.MapTemplate(
            group,
            "business-units",
            "BusinessUnits",
            MastersPermissions.BusinessUnitImport,
            BusinessUnitImportColumns.All);
    }
}
