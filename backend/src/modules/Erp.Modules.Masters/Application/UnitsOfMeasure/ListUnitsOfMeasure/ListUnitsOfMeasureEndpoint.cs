using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.UnitsOfMeasure.ListUnitsOfMeasure;

internal sealed class ListUnitsOfMeasureEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/units-of-measure", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListUnitsOfMeasureQuery, PagedResult<UnitOfMeasureListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListUnitsOfMeasureQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListUnitsOfMeasure")
            .WithSummary("List units of measure")
            .WithDescription("Includes each unit's decimal places and its conversion to the base unit of its family.")
            .RequirePermission(MastersPermissions.ReferenceDataRead)
            .Produces<PagedResult<UnitOfMeasureListItemDto>>();
    }
}
