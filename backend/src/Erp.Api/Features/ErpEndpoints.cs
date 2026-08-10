using Erp.Api.Features.BusinessUnits;
using Erp.Api.Features.BusinessUnits.ImportBusinessUnits;
using Erp.Api.Features.BusinessUnits.ListBusinessUnits;
using Erp.Api.Features.Customers;
using Erp.Api.Features.Customers.ImportCustomers;
using Erp.Api.Features.Customers.ListCustomers;
using Erp.Api.Features.Employees;
using Erp.Api.Features.Employees.ImportEmployees;
using Erp.Api.Features.Employees.ListEmployees;
using Erp.Api.Features.Roles;
using Erp.Api.Features.Roles.ImportRoles;
using Erp.Api.Features.Roles.ListRoles;
using Erp.Api.Features.Suppliers;
using Erp.Api.Features.Suppliers.ImportSuppliers;
using Erp.Api.Features.Suppliers.ListSuppliers;
using Microsoft.AspNetCore.Http;

namespace Erp.Api.Features;

public static class ErpEndpoints
{
    public static IEndpointRouteBuilder MapMasters(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var masters = endpoints
            .MapGroup("/api/v1/masters")
            .WithMetadata(new TagsAttribute("Masters"));

        new ImportSuppliersEndpoint().Map(masters);
        new ListSuppliersEndpoint().Map(masters);
        new SupplierWriteEndpoints().Map(masters);

        new CustomerWriteEndpoints().Map(masters);
        new ImportCustomersEndpoint().Map(masters);
        new ListCustomersEndpoint().Map(masters);

        new EmployeeWriteEndpoints().Map(masters);
        new ImportEmployeesEndpoint().Map(masters);
        new ListEmployeesEndpoint().Map(masters);

        new ImportRolesEndpoint().Map(masters);
        new ListRolesEndpoint().Map(masters);
        new RoleWriteEndpoints().Map(masters);

        new BusinessUnitWriteEndpoints().Map(masters);
        new ImportBusinessUnitsEndpoint().Map(masters);
        new ListBusinessUnitsEndpoint().Map(masters);

        return endpoints;
    }
}

