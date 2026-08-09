using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Roles.ListRoles;

internal sealed class ListRolesEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/roles", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListRolesQuery, PagedResult<RoleListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListRolesQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListMasterRoles")
            .WithSummary("List role master records")
            .WithDescription(
                "The legacy role master, which does NOT grant permissions — authorisation runs on "
                + "Identity roles. Server-paged, with free-text search across the role name.")
            .RequirePermission(MastersPermissions.RoleRead)
            .Produces<PagedResult<RoleListItemDto>>();
    }
}
