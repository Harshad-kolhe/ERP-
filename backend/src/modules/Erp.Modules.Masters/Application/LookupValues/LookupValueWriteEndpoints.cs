using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.LookupValues.WriteLookupValue;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.LookupValues;

internal sealed class SaveLookupValueValidator : AbstractValidator<SaveLookupValueRequest>
{
    public SaveLookupValueValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.");

        this.MaxLength(x => x.Name, 150, "Name");

        RuleFor(x => x.SortOrder)
            .InclusiveBetween(0, 9999)
            .WithMessage("Sort order must be between 0 and 9999.");
    }
}

internal sealed class CreateLookupValueValidator : AbstractValidator<CreateLookupValueRequest>
{
    public CreateLookupValueValidator()
    {
        Include(new SaveLookupValueValidator());

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("List is required.");

        this.MaxLength(x => x.Type, 50, "List");

        // The naming scheme the existing lists use: a bare word, or the owning
        // master then a dot. Enforced so a typo lands as a rejected save rather than
        // as a new empty list nothing ever asks for.
        this.Pattern(
            x => x.Type,
            "^[a-zA-Z][a-zA-Z0-9]*(\\.[a-zA-Z][a-zA-Z0-9]*)?$",
            "List must be a name like 'moc' or 'part.type' — letters and digits, at most one dot.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        this.MaxLength(x => x.Code, 50, "Code");
    }
}

internal sealed class UpdateLookupValueValidator : AbstractValidator<UpdateLookupValueRequest>
{
    public UpdateLookupValueValidator()
    {
        Include(new SaveLookupValueValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the option before updating it.");
    }
}

/// <summary>
/// Read-one, create and update for the option lists.
/// <para>
/// The screen that makes the rest of the reference data maintainable. Until this
/// existed, adding a material of construction meant a migration — which is the
/// legacy failure this table was built to end, reproduced one level up.
/// </para>
/// </summary>
internal sealed class LookupValueWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/lookup-values/{id:int}", async (
                int id,
                IQueryHandler<GetLookupValueByIdQuery, LookupValueDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetLookupValueByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetLookupValueById")
            .WithSummary("Get one reference-data option")
            .RequirePermission(MastersPermissions.ReferenceDataRead)
            .Produces<LookupValueDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/lookup-values", async (
                CreateLookupValueRequest request,
                ICommandHandler<CreateLookupValueCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateLookupValueCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/lookup-values/{id}", new { id }));
            })
            .WithName("CreateLookupValue")
            .WithSummary("Add an option to a list")
            .WithDescription("The option becomes selectable immediately — no deployment.")
            .RequirePermission(MastersPermissions.ReferenceDataCreate)
            .WithValidation<CreateLookupValueRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/lookup-values/{id:int}", async (
                int id,
                UpdateLookupValueRequest request,
                ICommandHandler<UpdateLookupValueCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateLookupValueCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateLookupValue")
            .WithSummary("Rename, reorder or retire an option")
            .WithDescription(
                "The list and the code cannot be changed: records store the code, so editing it would "
                + "reinterpret them. Retire an option by clearing Active — it stays for existing records "
                + "and drops out of the dropdown.")
            .RequirePermission(MastersPermissions.ReferenceDataUpdate)
            .WithValidation<UpdateLookupValueRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
