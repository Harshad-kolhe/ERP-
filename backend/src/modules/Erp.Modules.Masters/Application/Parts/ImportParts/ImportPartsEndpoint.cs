using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.ImportParts;

internal sealed class ImportPartsEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MasterImportEndpointBuilder.MapImport(
            group,
            "parts",
            MastersPermissions.PartImport,
            file => new ImportPartsCommand(file));

        MasterImportEndpointBuilder.MapTemplate(
            group,
            "parts",
            "Parts",
            MastersPermissions.PartImport,
            PartImportColumns.All);
    }
}
