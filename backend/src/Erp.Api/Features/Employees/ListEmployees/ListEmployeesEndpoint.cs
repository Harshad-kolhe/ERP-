using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Http;
using Erp.Api.Common.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Employees.ListEmployees;

public sealed class ListEmployeesEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/employees", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListEmployeesQuery, PagedResult<EmployeeListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListEmployeesQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListEmployees")
            .WithSummary("List employees")
            .WithDescription(
                "Server-paged. Supports sort=field:asc|desc (comma-separated), "
                + "filter=field:op:value (semicolon-separated), and free-text search across "
                + "first name, last name and email. Returns no payroll or credential field. "
                + "pageSize is clamped to 200.")
            .RequirePermission(MastersPermissions.EmployeeRead)
            .Produces<PagedResult<EmployeeListItemDto>>();
    }
}
