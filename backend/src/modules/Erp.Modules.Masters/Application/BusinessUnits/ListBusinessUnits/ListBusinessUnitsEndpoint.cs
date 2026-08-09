using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.BusinessUnits.ListBusinessUnits;

internal sealed class ListBusinessUnitsEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/business-units", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListBusinessUnitsQuery, PagedResult<BusinessUnitListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListBusinessUnitsQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListBusinessUnits")
            .WithSummary("List business units")
            .WithDescription(
                "Server-paged, with free-text search across business name, email and GSTN. "
                + "Returns every unit rather than the caller's own: this table defines the "
                + "tenancy boundary instead of sitting inside one, so the permission is the "
                + "only access control on it. pageSize is clamped to 200.")
            .RequirePermission(MastersPermissions.BusinessUnitRead)
            .Produces<PagedResult<BusinessUnitListItemDto>>();
    }
}
