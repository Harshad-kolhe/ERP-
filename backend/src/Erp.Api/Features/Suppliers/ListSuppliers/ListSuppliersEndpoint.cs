using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Http;
using Erp.Api.Common.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Suppliers.ListSuppliers;

public sealed class ListSuppliersEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/suppliers", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListSuppliersQuery, PagedResult<SupplierListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListSuppliersQuery(request), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListSuppliers")
            .WithSummary("List suppliers")
            .WithDescription(
                "Server-paged. Supports sort=field:asc|desc (comma-separated), "
                + "filter=field:op:value (semicolon-separated), and free-text search across "
                + "supplier code, name, email and GST number. pageSize is clamped to 200.")
            .RequirePermission(MastersPermissions.SupplierRead)
            .Produces<PagedResult<SupplierListItemDto>>();
    }
}
