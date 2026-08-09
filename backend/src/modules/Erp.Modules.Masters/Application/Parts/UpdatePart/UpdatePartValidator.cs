using System.Text.RegularExpressions;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Modules.Masters.Application.Parts.UpdatePart;

/// <summary>
/// Mirrors <c>CreatePartValidator</c>, minus the part number: it is the business
/// key and is not changed by an ordinary edit.
/// </summary>
internal sealed partial class UpdatePartValidator : AbstractValidator<UpdatePartRequest>
{
    public UpdatePartValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Description)
            .Must(value => value.Trim().Length <= 250)
            .WithMessage("Description must be 250 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.UnitOfMeasureCode)
            .NotEmpty().WithMessage("Unit of measure is required.");

        RuleFor(x => x.UnitOfMeasureCode)
            .Must(value => value.Trim().Length <= 10)
            .WithMessage("Unit of measure must be 10 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.UnitOfMeasureCode));

        RuleFor(x => x.HsnCode)
            .Must(value => HsnCodePattern().IsMatch(value!.Trim()))
            .WithMessage("HSN code must be 4, 6 or 8 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.HsnCode));

        RuleFor(x => x.DrawingNumber)
            .Must(value => value!.Trim().Length <= 50)
            .WithMessage("Drawing number must be 50 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.DrawingNumber));

        RuleFor(x => x.Attributes!)
            .SetValidator(new PartAttributesValidator())
            .When(x => x.Attributes is not null);

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the part before updating it.");
    }

    [GeneratedRegex("^[0-9]{4}([0-9]{2}([0-9]{2})?)?$")]
    private static partial Regex HsnCodePattern();
}
