using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using FluentValidation;

namespace Erp.Modules.Masters.Application.ParentParts.WriteParentPart;

/// <summary>
/// One component line, checked for shape. Whether the part exists, is unique on the
/// build and is not the parent needs the database and lives in
/// <see cref="ParentPartComposition"/>.
/// </summary>
internal sealed class ParentPartComponentValidator : AbstractValidator<ParentPartComponentDto>
{
    public ParentPartComponentValidator()
    {
        RuleFor(x => x.PartId)
            .NotEmpty().WithMessage("Choose a component part.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0m).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(999_999_999m).WithMessage("Quantity is too large.");

        RuleFor(x => x.UnitWeightKg)
            .InclusiveBetween(0m, 9_999_999.9999m)
            .WithMessage("Unit weight must be between 0 and 9,999,999.9999 kg.");

        this.Money(x => x.Rate, "Rate");
        this.MaxLength(x => x.UnitOfMeasureCode, 10, "Unit of measure");
        this.MaxLength(x => x.DrawingNumber, 50, "Drawing number");
        this.MaxLength(x => x.Remark, 500, "Remark");
    }
}

/// <summary>
/// The single server-side authority on what a valid new parent part looks like.
/// <para>
/// The header rules are restated in <see cref="UpdateParentPartValidator"/> rather
/// than shared through a helper, because FluentValidation derives the error key
/// from the property expression: routed through a delegate, every message would
/// come back under a name the form has no field for, and the user would be told
/// something is wrong without being told what.
/// </para>
/// </summary>
internal sealed class CreateParentPartValidator : AbstractValidator<CreateParentPartRequest>
{
    public CreateParentPartValidator()
    {
        RuleFor(x => x.PartId)
            .NotEmpty().WithMessage("Choose the part this build is for.");

        this.MaxLength(x => x.Description, 255, "Description");
        this.MaxLength(x => x.UnitOfMeasureCode, 10, "Unit of measure");
        this.MaxLength(x => x.DrawingNumber, 50, "Drawing number");
        this.MaxLength(x => x.Category, 50, "Category");

        // Bounded here as well as in the handler, so an oversized payload is
        // rejected before anything queries the part master with it.
        RuleFor(x => x.Components)
            .NotNull().WithMessage("Send an empty list rather than nothing when a build has no components.")
            .Must(components => components.Count <= ParentPartComposition.MaxComponents)
            .WithMessage($"A build may have at most {ParentPartComposition.MaxComponents} component lines.");

        RuleForEach(x => x.Components).SetValidator(new ParentPartComponentValidator());
    }
}

/// <summary>
/// Mirrors <see cref="CreateParentPartValidator"/>, minus the part: which part a
/// build describes is its identity, not one of its fields.
/// </summary>
internal sealed class UpdateParentPartValidator : AbstractValidator<UpdateParentPartRequest>
{
    public UpdateParentPartValidator()
    {
        this.MaxLength(x => x.Description, 255, "Description");
        this.MaxLength(x => x.UnitOfMeasureCode, 10, "Unit of measure");
        this.MaxLength(x => x.DrawingNumber, 50, "Drawing number");
        this.MaxLength(x => x.Category, 50, "Category");

        RuleFor(x => x.Components)
            .NotNull().WithMessage("Send an empty list rather than nothing when a build has no components.")
            .Must(components => components.Count <= ParentPartComposition.MaxComponents)
            .WithMessage($"A build may have at most {ParentPartComposition.MaxComponents} component lines.");

        RuleForEach(x => x.Components).SetValidator(new ParentPartComponentValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the parent part before updating it.");
    }
}
