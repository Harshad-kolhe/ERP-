using Erp.Api.Common.Modules;
using Erp.Api.Features.Imports;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Employees.ImportEmployees;

public sealed class ImportEmployeesEndpoint : IEndpoint
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
