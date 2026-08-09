using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Application.UnitsOfMeasure.WriteUnitOfMeasure;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.UnitsOfMeasure;

internal sealed class SaveUnitOfMeasureValidator : AbstractValidator<SaveUnitOfMeasureRequest>
{
    public SaveUnitOfMeasureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.");

        this.MaxLength(x => x.Name, 100, "Name");

        // Six is the quantity scale the database stores; a unit claiming more
        // precision than the column has would round without saying so.
        RuleFor(x => x.Decimals)
            .InclusiveBetween(0, 6)
            .WithMessage("Decimals must be between 0 and 6.");

        this.MaxLength(x => x.BaseUnitCode, 10, "Base unit");

        RuleFor(x => x.ConversionToBase)
            .GreaterThan(0m)
            .WithMessage("Conversion factor must be greater than zero.")
            .When(x => x.ConversionToBase is not null);

        RuleFor(x => x.SortOrder)
            .InclusiveBetween(0, 9999)
            .WithMessage("Sort order must be between 0 and 9999.");
    }
}

internal sealed class CreateUnitOfMeasureValidator : AbstractValidator<CreateUnitOfMeasureRequest>
{
    public CreateUnitOfMeasureValidator()
    {
        Include(new SaveUnitOfMeasureValidator());

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        this.MaxLength(x => x.Code, 10, "Code");

        this.Pattern(
            x => x.Code,
            "^[A-Za-z][A-Za-z0-9]*$",
            "Code may contain only letters and digits, starting with a letter.");
    }
}

internal sealed class UpdateUnitOfMeasureValidator : AbstractValidator<UpdateUnitOfMeasureRequest>
{
    public UpdateUnitOfMeasureValidator()
    {
        Include(new SaveUnitOfMeasureValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the unit before updating it.");
    }
}

/// <summary>Read-one, create and update for units of measure.</summary>
internal sealed class UnitOfMeasureWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/units-of-measure/{id:int}", async (
                int id,
                IQueryHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetUnitOfMeasureByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetUnitOfMeasureById")
            .WithSummary("Get one unit of measure")
            .RequirePermission(MastersPermissions.ReferenceDataRead)
            .Produces<UnitOfMeasureDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/units-of-measure", async (
                CreateUnitOfMeasureRequest request,
                ICommandHandler<CreateUnitOfMeasureCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateUnitOfMeasureCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/units-of-measure/{id}", new { id }));
            })
            .WithName("CreateUnitOfMeasure")
            .WithSummary("Create a unit of measure")
            .WithDescription(
                "Leave the base unit blank for a unit that is itself a base. A base unit must not "
                + "itself convert to another — conversion is one level, not a chain.")
            .RequirePermission(MastersPermissions.ReferenceDataCreate)
            .WithValidation<CreateUnitOfMeasureRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/units-of-measure/{id:int}", async (
                int id,
                UpdateUnitOfMeasureRequest request,
                ICommandHandler<UpdateUnitOfMeasureCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateUnitOfMeasureCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateUnitOfMeasure")
            .WithSummary("Edit a unit of measure")
            .WithDescription("The code cannot be changed: parts store the letters, not a key.")
            .RequirePermission(MastersPermissions.ReferenceDataUpdate)
            .WithValidation<UpdateUnitOfMeasureRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
