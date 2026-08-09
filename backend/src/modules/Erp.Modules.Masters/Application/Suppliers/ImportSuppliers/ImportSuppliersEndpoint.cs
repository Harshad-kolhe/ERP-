using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Suppliers.ImportSuppliers;

internal sealed class ImportSuppliersEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MasterImportEndpointBuilder.MapImport(
            group,
            "suppliers",
            MastersPermissions.SupplierImport,
            file => new ImportSuppliersCommand(file));

        MasterImportEndpointBuilder.MapTemplate(
            group,
            "suppliers",
            "Suppliers",
            MastersPermissions.SupplierImport,
            SupplierImportColumns.All);
    }
}
