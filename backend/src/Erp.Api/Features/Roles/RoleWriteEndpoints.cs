using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Http;
using Erp.Api.Common.Modules;
using Erp.Contracts.Masters;
using Erp.Api.Features.Masters;
using Erp.Api.Features.Roles.WriteRole;
using Erp.Api.Common.Security;
using Erp.Api.Common.Results;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Roles;

public sealed class SaveRoleMasterValidator : AbstractValidator<SaveRoleMasterRequest>
{
    public SaveRoleMasterValidator()
    {
        RuleFor(x => x.RolesName)
            .NotEmpty().WithMessage("Role name is required.");

        this.MaxLength(x => x.RolesName, 100, "Role name");

        // Letters, digits and spaces. These names are printed on documents and used
        // as lookup keys; punctuation in them has only ever caused trouble.
        this.Pattern(
            x => x.RolesName,
            "^[0-9A-Za-z ]+$",
            "Role name may contain only letters, digits and spaces.");
    }
}

public sealed class CreateRoleMasterValidator : AbstractValidator<CreateRoleMasterRequest>
{
    public CreateRoleMasterValidator()
    {
        Include(new SaveRoleMasterValidator());

        RuleFor(x => x.RoleId)
            .GreaterThan(0)
            .WithMessage("Role id must be a positive number.");
    }
}

public sealed class UpdateRoleMasterValidator : AbstractValidator<UpdateRoleMasterRequest>
{
    public UpdateRoleMasterValidator()
    {
        Include(new SaveRoleMasterValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the role before updating it.");
    }
}

/// <summary>
/// Read-one, create and update for the legacy role master.
/// <para>
/// Not the permissions screen. These rows are what <c>Employee.RoleId</c> points
/// at; what a role may <em>do</em> is decided on the Identity roles screen under
/// <c>/admin/roles</c>.
/// </para>
/// </summary>
public sealed class RoleWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/roles/{id:int}", async (
                int id,
                IQueryHandler<GetRoleMasterByIdQuery, RoleMasterDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetRoleMasterByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetRoleMasterById")
            .WithSummary("Get one legacy role master row")
            .RequirePermission(MastersPermissions.RoleRead)
            .Produces<RoleMasterDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/roles", async (
                CreateRoleMasterRequest request,
                ICommandHandler<CreateRoleMasterCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateRoleMasterCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/roles/{id}", new { id }));
            })
            .WithName("CreateRoleMaster")
            .WithSummary("Create a legacy role master row")
            .WithDescription("Grants no permissions. Permissions are assigned on the roles administration screen.")
            .RequirePermission(MastersPermissions.RoleCreate)
            .WithValidation<CreateRoleMasterRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/roles/{id:int}", async (
                int id,
                UpdateRoleMasterRequest request,
                ICommandHandler<UpdateRoleMasterCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateRoleMasterCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateRoleMaster")
            .WithSummary("Update a legacy role master row")
            .WithDescription("Requires the rowVersion returned by GET. The role id cannot be changed.")
            .RequirePermission(MastersPermissions.RoleUpdate)
            .WithValidation<UpdateRoleMasterRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
