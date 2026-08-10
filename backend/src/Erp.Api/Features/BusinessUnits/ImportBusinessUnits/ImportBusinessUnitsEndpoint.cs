using Erp.Api.Common.Modules;
using Erp.Api.Features.Imports;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.BusinessUnits.ImportBusinessUnits;

public sealed class ImportBusinessUnitsEndpoint : IEndpoint
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
