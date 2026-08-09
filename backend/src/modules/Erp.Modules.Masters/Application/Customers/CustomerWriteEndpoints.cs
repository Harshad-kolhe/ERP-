using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Customers.WriteCustomer;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Customers;

/// <summary>Read-one, create and update for customers.</summary>
internal sealed class CustomerWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/customers/{id:int}", async (
                int id,
                IQueryHandler<GetCustomerByIdQuery, CustomerDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetCustomerByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetCustomerById")
            .WithSummary("Get one customer")
            .WithDescription("Returns every editable field plus the rowVersion the update endpoint requires.")
            .RequirePermission(MastersPermissions.CustomerRead)
            .Produces<CustomerDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/customers", async (
                CreateCustomerRequest request,
                ICommandHandler<CreateCustomerCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateCustomerCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/customers/{id}", new { id }));
            })
            .WithName("CreateCustomer")
            .WithSummary("Create a customer")
            .RequirePermission(MastersPermissions.CustomerCreate)
            .WithValidation<CreateCustomerRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/customers/{id:int}", async (
                int id,
                UpdateCustomerRequest request,
                ICommandHandler<UpdateCustomerCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateCustomerCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateCustomer")
            .WithSummary("Update a customer")
            .WithDescription(
                "Requires the rowVersion returned by GET. A stale value yields 409. "
                + "The customer code cannot be changed here.")
            .RequirePermission(MastersPermissions.CustomerUpdate)
            .WithValidation<UpdateCustomerRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
