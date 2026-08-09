using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.CreatePart;

internal sealed class CreatePartEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/parts", async (
                CreatePartRequest request,
                ICommandHandler<CreatePartCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreatePartCommand(
                    request.PartNumber,
                    request.Description,
                    request.CategoryId,
                    request.UnitOfMeasureCode,
                    request.HsnCode,
                    request.DrawingNumber,
                    request.Attributes);

                var result = await handler.HandleAsync(command, cancellationToken);

                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/parts/{id}", new { id }));
            })
            .WithName("CreatePart")
            .WithSummary("Create a part")
            .WithDescription("Creates the part in Draft status. It becomes usable once approved.")
            .RequirePermission(MastersPermissions.PartCreate)
            .WithValidation<CreatePartRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
