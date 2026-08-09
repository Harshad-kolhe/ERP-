using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.HsnCodes.ListHsnCodes;

internal sealed class ListHsnCodesEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/hsn-codes", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListHsnCodesQuery, PagedResult<HsnCodeListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListHsnCodesQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListHsnCodes")
            .WithSummary("List HSN codes")
            .WithDescription("Each row shows the GST rate in force today; the full rate history is on the detail.")
            .RequirePermission(MastersPermissions.ReferenceDataRead)
            .Produces<PagedResult<HsnCodeListItemDto>>();
    }
}
