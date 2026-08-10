using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Http;
using Erp.Api.Common.Modules;
using Erp.Contracts.Masters;
using Erp.Api.Features.BusinessUnits.WriteBusinessUnit;
using Erp.Api.Features.Masters;
using Erp.Api.Common.Security;
using Erp.Api.Common.Results;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.BusinessUnits;

/// <summary>Rules for a business unit. Lengths mirror <c>BusinessUnitConfiguration</c>.</summary>
public sealed class SaveBusinessUnitValidator : AbstractValidator<SaveBusinessUnitRequest>
{
    public SaveBusinessUnitValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.");

        this.MaxLength(x => x.BusinessName, 200, "Business name");
        this.MaxLength(x => x.Address, 500, "Address");
        this.MaxLength(x => x.StateName, 100, "State name");
        this.MaxLength(x => x.StateCode, 10, "State code");
        this.MaxLength(x => x.ContactNumber, 30, "Contact number");
        this.MaxLength(x => x.Website, 200, "Website");

        this.Email(x => x.Email, "Email");
        this.Gstin(x => x.Gstn);
        this.Pan(x => x.Pan);

        // 21 characters exactly â€” a CIN that is any other length is not a CIN.
        this.Pattern(x => x.Cin, "^[A-Za-z0-9]{21}$", "CIN must be 21 characters.");
    }
}

public sealed class CreateBusinessUnitValidator : AbstractValidator<CreateBusinessUnitRequest>
{
    public CreateBusinessUnitValidator()
    {
        Include(new SaveBusinessUnitValidator());

        RuleFor(x => x.BusinessUnitId)
            .GreaterThan(0)
            .WithMessage("Unit id must be a positive number.");
    }
}

public sealed class UpdateBusinessUnitValidator : AbstractValidator<UpdateBusinessUnitRequest>
{
    public UpdateBusinessUnitValidator()
    {
        Include(new SaveBusinessUnitValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the business unit before updating it.");
    }
}

/// <summary>Read-one, create and update for business units.</summary>
public sealed class BusinessUnitWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/business-units/{id:int}", async (
                int id,
                IQueryHandler<GetBusinessUnitByIdQuery, BusinessUnitDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetBusinessUnitByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetBusinessUnitById")
            .WithSummary("Get one business unit")
            .RequirePermission(MastersPermissions.BusinessUnitRead)
            .Produces<BusinessUnitDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/business-units", async (
                CreateBusinessUnitRequest request,
                ICommandHandler<CreateBusinessUnitCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateBusinessUnitCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/business-units/{id}", new { id }));
            })
            .WithName("CreateBusinessUnit")
            .WithSummary("Create a business unit")
            .WithDescription("The unit id is the value every other table carries in its tenancy column.")
            .RequirePermission(MastersPermissions.BusinessUnitCreate)
            .WithValidation<CreateBusinessUnitRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/business-units/{id:int}", async (
                int id,
                UpdateBusinessUnitRequest request,
                ICommandHandler<UpdateBusinessUnitCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateBusinessUnitCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateBusinessUnit")
            .WithSummary("Update a business unit")
            .WithDescription(
                "Requires the rowVersion returned by GET. The unit id cannot be changed â€” every "
                + "record in the system points at it.")
            .RequirePermission(MastersPermissions.BusinessUnitUpdate)
            .WithValidation<UpdateBusinessUnitRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
