using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Application.Suppliers.WriteSupplier;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Suppliers;

/// <summary>Read-one, create and update for suppliers.</summary>
internal sealed class SupplierWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/suppliers/{id:int}", async (
                int id,
                IQueryHandler<GetSupplierByIdQuery, SupplierDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetSupplierByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetSupplierById")
            .WithSummary("Get one supplier")
            .WithDescription("Returns every editable field plus the rowVersion the update endpoint requires.")
            .RequirePermission(MastersPermissions.SupplierRead)
            .Produces<SupplierDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/suppliers", async (
                CreateSupplierRequest request,
                ICommandHandler<CreateSupplierCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateSupplierCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/suppliers/{id}", new { id }));
            })
            .WithName("CreateSupplier")
            .WithSummary("Create a supplier")
            .WithDescription("Creates the supplier in the status supplied, defaulting to Draft.")
            .RequirePermission(MastersPermissions.SupplierCreate)
            .WithValidation<CreateSupplierRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/suppliers/{id:int}", async (
                int id,
                UpdateSupplierRequest request,
                ICommandHandler<UpdateSupplierCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateSupplierCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateSupplier")
            .WithSummary("Update a supplier")
            .WithDescription(
                "Requires the rowVersion returned by GET. A stale value yields 409 rather than "
                + "overwriting a concurrent edit. The supplier code cannot be changed here.")
            .RequirePermission(MastersPermissions.SupplierUpdate)
            .WithValidation<UpdateSupplierRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
