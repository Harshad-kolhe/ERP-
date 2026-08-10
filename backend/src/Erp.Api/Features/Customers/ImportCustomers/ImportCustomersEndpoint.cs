using Erp.Api.Common.Modules;
using Erp.Api.Features.Imports;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Customers.ImportCustomers;

public sealed class ImportCustomersEndpoint : IEndpoint
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
