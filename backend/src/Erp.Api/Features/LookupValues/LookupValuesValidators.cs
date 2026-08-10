using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.LookupValues;

public sealed class SaveLookupValueValidator : AbstractValidator<SaveLookupValueRequest>
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

public sealed class CreateLookupValueValidator : AbstractValidator<CreateLookupValueRequest>
{
    public CreateLookupValueValidator()
    {
        Include(new SaveLookupValueValidator());

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("List is required.");

        this.MaxLength(x => x.Type, 50, "List");

        this.Pattern(
            x => x.Type,
            "^[a-zA-Z][a-zA-Z0-9]*(\\.[a-zA-Z][a-zA-Z0-9]*)?$",
            "List must be a name like 'moc' or 'part.type' - letters and digits, at most one dot.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        this.MaxLength(x => x.Code, 50, "Code");
    }
}

public sealed class UpdateLookupValueValidator : AbstractValidator<UpdateLookupValueRequest>
{
    public UpdateLookupValueValidator()
    {
        Include(new SaveLookupValueValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the option before updating it.");
    }
}
