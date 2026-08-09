using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Employees.WriteEmployee;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Employees;

/// <summary>Read-one, create and update for employees.</summary>
internal sealed class EmployeeWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/employees/{id:int}", async (
                int id,
                IQueryHandler<GetEmployeeByIdQuery, EmployeeDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetEmployeeByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetEmployeeById")
            .WithSummary("Get one employee")
            .WithDescription(
                "Carries no credential. Pay fields are null unless the caller holds "
                + "masters.employee.payroll.read; canEditPayroll says which case it is.")
            .RequirePermission(MastersPermissions.EmployeeRead)
            .Produces<EmployeeDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/employees", async (
                CreateEmployeeRequest request,
                ICommandHandler<CreateEmployeeCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateEmployeeCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/employees/{id}", new { id }));
            })
            .WithName("CreateEmployee")
            .WithSummary("Create an employee")
            .WithDescription("Pay fields are ignored without masters.employee.payroll.read.")
            .RequirePermission(MastersPermissions.EmployeeCreate)
            .WithValidation<CreateEmployeeRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/employees/{id:int}", async (
                int id,
                UpdateEmployeeRequest request,
                ICommandHandler<UpdateEmployeeCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateEmployeeCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateEmployee")
            .WithSummary("Update an employee")
            .WithDescription(
                "Requires the rowVersion returned by GET. Pay fields are left untouched — not "
                + "cleared — for a caller without masters.employee.payroll.read.")
            .RequirePermission(MastersPermissions.EmployeeUpdate)
            .WithValidation<UpdateEmployeeRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
