using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Lookups.GetLookups;

internal sealed class GetLookupsEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/lookups", async (
                string? types,
                IQueryHandler<GetLookupsQuery, LookupSetDto> handler,
                CancellationToken cancellationToken) =>
            {
                var requested = (types ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
                var result = await handler.HandleAsync(new GetLookupsQuery(requested), cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("GetLookups")
            .WithSummary("Option lists for master forms")
            .WithDescription(
                "Comma-separated list names, e.g. ?types=uom,currency,supplier.type. Returns the "
                + "active options of each in display order. This is the only source of dropdown "
                + "options in the system — the web app holds none of its own.")
            // Authenticated, but not permission-gated. These are the names of units of
            // measure and currencies: knowing that "NOS" exists reveals nothing, and
            // every list is rendered inside a screen the caller already had to be
            // allowed to open. The guarded resources are the create and update
            // endpoints that consume the codes, not the codes themselves. Gating this
            // as well would mean any role missing one extra grant silently gets a form
            // full of empty dropdowns.
            .RequireAuthenticatedUserOnly()
            .Produces<LookupSetDto>();
    }
}
