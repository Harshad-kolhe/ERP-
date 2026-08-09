using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Parts.ApprovePart;

internal sealed class ApprovePartEndpoint : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/parts/{id:guid}/approve", async (
                Guid id,
                ICommandHandler<ApprovePartCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ApprovePartCommand(id), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("ApprovePart")
            .WithSummary("Approve a part")
            .WithDescription("Approves a part awaiting review. The author cannot approve their own part.")
            .RequirePermission(MastersPermissions.PartApprove)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
