using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Application.Imports;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Employees.ImportEmployees;

internal sealed class ImportEmployeesEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MasterImportEndpointBuilder.MapImport(
            group,
            "employees",
            MastersPermissions.EmployeeImport,
            file => new ImportEmployeesCommand(file));

        MasterImportEndpointBuilder.MapTemplate(
            group,
            "employees",
            "Employees",
            MastersPermissions.EmployeeImport,
            EmployeeImportColumns.All);
    }
}
