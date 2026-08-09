using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Customers.ImportCustomers;

internal sealed class ImportCustomersEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MasterImportEndpointBuilder.MapImport(
            group,
            "customers",
            MastersPermissions.CustomerImport,
            file => new ImportCustomersCommand(file));

        MasterImportEndpointBuilder.MapTemplate(
            group,
            "customers",
            "Customers",
            MastersPermissions.CustomerImport,
            CustomerImportColumns.All);
    }
}
