using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.GetPartById;

internal sealed class GetPartByIdEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/parts/{id:guid}", async (
                Guid id,
                IQueryHandler<GetPartByIdQuery, PartDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetPartByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetPartById")
            .WithSummary("Get a part")
            .RequirePermission(MastersPermissions.PartRead)
            .Produces<PartDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
