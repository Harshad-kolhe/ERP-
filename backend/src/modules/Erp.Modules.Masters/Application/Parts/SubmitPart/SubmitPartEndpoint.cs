using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.SubmitPart;

internal sealed class SubmitPartEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/parts/{id:guid}/submit", async (
                Guid id,
                ICommandHandler<SubmitPartCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new SubmitPartCommand(id), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("SubmitPartForApproval")
            .WithSummary("Submit a part for approval")
            .WithDescription("Moves a Draft part to PendingApproval. It cannot be edited while under review.")
            .RequirePermission(MastersPermissions.PartSubmit)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
