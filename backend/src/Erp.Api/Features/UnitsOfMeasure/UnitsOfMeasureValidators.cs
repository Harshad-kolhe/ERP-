using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.UnitsOfMeasure;

public sealed class SaveUnitOfMeasureValidator : AbstractValidator<SaveUnitOfMeasureRequest>
{
    public SaveUnitOfMeasureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.");

        this.MaxLength(x => x.Name, 100, "Name");

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

public sealed class CreateUnitOfMeasureValidator : AbstractValidator<CreateUnitOfMeasureRequest>
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

public sealed class UpdateUnitOfMeasureValidator : AbstractValidator<UpdateUnitOfMeasureRequest>
{
    public UpdateUnitOfMeasureValidator()
    {
        Include(new SaveUnitOfMeasureValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the unit before updating it.");
    }
}
