using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.LookupValues.ListLookupValues;

internal sealed class ListLookupValuesEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/lookup-values", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListLookupValuesQuery, PagedResult<LookupValueListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListLookupValuesQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListLookupValues")
            .WithSummary("List reference-data options")
            .WithDescription(
                "Every dropdown option in the system, across all lists. Filter on `type` to see one "
                + "list. Not the endpoint a form fills its dropdowns from — that is GET /masters/lookups.")
            .RequirePermission(MastersPermissions.ReferenceDataRead)
            .Produces<PagedResult<LookupValueListItemDto>>();
    }
}
