using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.UpdatePart;

internal sealed class UpdatePartEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPut("/parts/{id:guid}", async (
                Guid id,
                UpdatePartRequest request,
                ICommandHandler<UpdatePartCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdatePartCommand(
                    id,
                    request.Description,
                    request.CategoryId,
                    request.UnitOfMeasureCode,
                    request.HsnCode,
                    request.DrawingNumber,
                    request.Attributes,
                    request.RowVersion);

                var result = await handler.HandleAsync(command, cancellationToken);

                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdatePart")
            .WithSummary("Update a part")
            .WithDescription(
                "Requires the rowVersion returned by GET. A stale value yields 409 rather than "
                + "overwriting a concurrent edit. The part number cannot be changed here.")
            .RequirePermission(MastersPermissions.PartUpdate)
            .WithValidation<UpdatePartRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
