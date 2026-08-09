using System.Text.RegularExpressions;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Modules.Masters.Application.Parts.CreatePart;

/// <summary>
/// The single server-side authority on what a valid new part looks like.
/// <para>
/// Applied automatically by <c>ValidationFilter</c> before the handler runs. The
/// Zod schema in the web app mirrors these rules so the user gets immediate
/// feedback, but it is a convenience — nothing reaches the domain without passing
/// through here first.
/// </para>
/// <para>
/// It validates the <em>contract</em> type, not the command: this is the shape
/// that actually arrives over HTTP, so a malformed body is rejected before any
/// mapping code runs on it.
/// </para>
/// </summary>
internal sealed partial class CreatePartValidator : AbstractValidator<CreatePartRequest>
{
    public CreatePartValidator()
    {
        RuleFor(x => x.PartNumber)
            .NotEmpty().WithMessage("Part number is required.");

        // Format rules run against the trimmed value, because that is what
        // Part.Create stores. Otherwise a part number pasted from a spreadsheet
        // with a trailing space is rejected as malformed, which tells the user
        // nothing useful about what is actually wrong with it.
        RuleFor(x => x.PartNumber)
            .Must(value => value.Trim().Length <= 50)
            .WithMessage("Part number must be 50 characters or fewer.")
            .Must(value => PartNumberPattern().IsMatch(value.Trim()))
            .WithMessage("Part number may contain only letters, digits, dot, underscore, slash and hyphen.")
            .When(x => !string.IsNullOrWhiteSpace(x.PartNumber));

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

        // HSN codes are 4, 6 or 8 digits under the Indian GST schedule. Rejecting
        // anything else here stops an unfilable invoice being generated months later.
        RuleFor(x => x.HsnCode)
            .Must(value => HsnCodePattern().IsMatch(value!.Trim()))
            .WithMessage("HSN code must be 4, 6 or 8 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.HsnCode));

        RuleFor(x => x.DrawingNumber)
            .Must(value => value!.Trim().Length <= 50)
            .WithMessage("Drawing number must be 50 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.DrawingNumber));

        // Delegated rather than restated, so create and update cannot drift apart
        // on what a valid weight or technical specification is.
        RuleFor(x => x.Attributes!)
            .SetValidator(new PartAttributesValidator())
            .When(x => x.Attributes is not null);
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._/-]*$")]
    private static partial Regex PartNumberPattern();

    [GeneratedRegex("^[0-9]{4}([0-9]{2}([0-9]{2})?)?$")]
    private static partial Regex HsnCodePattern();
}
