using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.HsnCodes.WriteHsnCode;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.HsnCodes;

internal sealed class SaveHsnCodeValidator : AbstractValidator<SaveHsnCodeRequest>
{
    public SaveHsnCodeValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        this.MaxLength(x => x.Description, 250, "Description");
    }
}

/// <summary>The rate rules, shared by creating a code and amending one.</summary>
internal static class HsnRateRules
{
    public static void AddTo<T>(AbstractValidator<T> validator, Func<T, decimal> rate)
    {
        // 0 to 100. GST rates are 0, 5, 12, 18 and 28 today, but the schedule is
        // the Council's to change and hard-coding the current five would make the
        // next one a deployment — which is the whole failure this screen ends.
        validator.RuleFor(x => rate(x))
            .InclusiveBetween(0m, 100m)
            .WithName("ratePercent")
            .WithMessage("Rate must be between 0 and 100 percent.");
    }
}

internal sealed class CreateHsnCodeValidator : AbstractValidator<CreateHsnCodeRequest>
{
    public CreateHsnCodeValidator()
    {
        Include(new SaveHsnCodeValidator());

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        // The same rule Part enforces on the code it stores, so a code that can be
        // created is always a code a part can carry.
        this.Pattern(
            x => x.Code,
            "^[0-9]{4}([0-9]{2}([0-9]{2})?)?$",
            "HSN code must be 4, 6 or 8 digits.");

        HsnRateRules.AddTo(this, x => x.RatePercent);
    }
}

internal sealed class UpdateHsnCodeValidator : AbstractValidator<UpdateHsnCodeRequest>
{
    public UpdateHsnCodeValidator()
    {
        Include(new SaveHsnCodeValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the code before updating it.");
    }
}

internal sealed class AddHsnGstRateValidator : AbstractValidator<AddHsnGstRateRequest>
{
    public AddHsnGstRateValidator()
    {
        HsnRateRules.AddTo(this, x => x.RatePercent);
    }
}

/// <summary>
/// Read-one, create, update and rate amendment for HSN codes.
/// <para>
/// Four routes rather than three because a rate change is not an edit of the code.
/// Rates are appended and never rewritten — see <c>AddHsnGstRateHandler</c>.
/// </para>
/// </summary>
internal sealed class HsnCodeWriteEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/hsn-codes/{id:int}", async (
                int id,
                IQueryHandler<GetHsnCodeByIdQuery, HsnCodeDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetHsnCodeByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetHsnCodeById")
            .WithSummary("Get one HSN code and its rate history")
            .RequirePermission(MastersPermissions.ReferenceDataRead)
            .Produces<HsnCodeDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/hsn-codes", async (
                CreateHsnCodeRequest request,
                ICommandHandler<CreateHsnCodeCommand, int> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateHsnCodeCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/hsn-codes/{id}", new { id }));
            })
            .WithName("CreateHsnCode")
            .WithSummary("Create an HSN code with its opening rate")
            .WithDescription("The rate is required: a code with none would tax an invoice line at nothing.")
            .RequirePermission(MastersPermissions.ReferenceDataCreate)
            .WithValidation<CreateHsnCodeRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/hsn-codes/{id:int}", async (
                int id,
                UpdateHsnCodeRequest request,
                ICommandHandler<UpdateHsnCodeCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateHsnCodeCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateHsnCode")
            .WithSummary("Edit an HSN code's description or active flag")
            .WithDescription("Neither the code nor its rates change here — post a rate to amend the tax.")
            .RequirePermission(MastersPermissions.ReferenceDataUpdate)
            .WithValidation<UpdateHsnCodeRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/hsn-codes/{id:int}/rates", async (
                int id,
                AddHsnGstRateRequest request,
                ICommandHandler<AddHsnGstRateCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new AddHsnGstRateCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("AddHsnGstRate")
            .WithSummary("Record a GST rate change")
            .WithDescription(
                "Appends a rate from a date. Existing rates are never edited: a document keeps the "
                + "rate that applied when it was raised. Correct a wrong rate by superseding it.")
            .RequirePermission(MastersPermissions.ReferenceDataUpdate)
            .WithValidation<AddHsnGstRateRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
