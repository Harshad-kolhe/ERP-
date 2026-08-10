using Erp.Api.Common.Modules;
using Erp.Api.Features.Imports;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Suppliers.ImportSuppliers;

public sealed class ImportSuppliersEndpoint : IEndpoint
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
