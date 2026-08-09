using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.ListParts;

internal sealed class ListPartsEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/parts", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListPartsQuery, PagedResult<PartListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListPartsQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListParts")
            .WithSummary("List parts")
            .WithDescription(
                "Server-paged. Supports sort=field:asc|desc (comma-separated), "
                + "filter=field:op:value (semicolon-separated), and free-text search across "
                + "part number, item code, description and HSN code. pageSize is clamped to 200.")
            .RequirePermission(MastersPermissions.PartRead)
            .Produces<PagedResult<PartListItemDto>>();
    }
}
