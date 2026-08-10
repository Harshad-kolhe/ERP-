using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.ParentParts;

public sealed class ParentPartComponentValidator : AbstractValidator<ParentPartComponentDto>
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

public sealed class CreateParentPartValidator : AbstractValidator<CreateParentPartRequest>
{
    public CreateParentPartValidator()
    {
        RuleFor(x => x.PartId)
            .NotEmpty().WithMessage("Choose the part this build is for.");

        this.MaxLength(x => x.Description, 255, "Description");
        this.MaxLength(x => x.UnitOfMeasureCode, 10, "Unit of measure");
        this.MaxLength(x => x.DrawingNumber, 50, "Drawing number");
        this.MaxLength(x => x.Category, 50, "Category");

        RuleFor(x => x.Components)
            .NotNull().WithMessage("Send an empty list rather than nothing when a build has no components.")
            .Must(components => components.Count <= ParentPartComposition.MaxComponents)
            .WithMessage($"A build may have at most {ParentPartComposition.MaxComponents} component lines.");

        RuleForEach(x => x.Components).SetValidator(new ParentPartComponentValidator());
    }
}

public sealed class UpdateParentPartValidator : AbstractValidator<UpdateParentPartRequest>
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
