using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.Parts;

public static partial class PartNumberFormat
{
    public const int MaxPartNumberLength = 50;

    public const string PartNumberRule =
        "Part number may contain only letters, digits, dot, underscore, slash and hyphen.";

    public const string HsnCodeRule = "HSN code must be 4, 6 or 8 digits.";

    public static bool IsValidPartNumber(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= MaxPartNumberLength
        && PartNumberPattern().IsMatch(value.Trim());

    public static bool IsValidHsnCode(string value) =>
        !string.IsNullOrWhiteSpace(value) && HsnCodePattern().IsMatch(value.Trim());

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._/-]*$")]
    private static partial Regex PartNumberPattern();

    [GeneratedRegex("^[0-9]{4}([0-9]{2}([0-9]{2})?)?$")]
    private static partial Regex HsnCodePattern();
}

public sealed partial class CreatePartValidator : AbstractValidator<CreatePartRequest>
{
    public CreatePartValidator()
    {
        RuleFor(x => x.PartNumber)
            .NotEmpty().WithMessage("Part number is required.");

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
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._/-]*$")]
    private static partial Regex PartNumberPattern();

    [GeneratedRegex("^[0-9]{4}([0-9]{2}([0-9]{2})?)?$")]
    private static partial Regex HsnCodePattern();
}

public sealed partial class UpdatePartValidator : AbstractValidator<UpdatePartRequest>
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

public sealed class PartAttributesValidator : AbstractValidator<PartAttributesDto>
{
    public PartAttributesValidator()
    {
        MaxLength(x => x.ItemNumber, 50, "Item code");
        MaxLength(x => x.TechnicalSpecification, 2000, "Technical specification");
        MaxLength(x => x.Moc, 50, "MOC");
        MaxLength(x => x.PartCategoryCode, 50, "Part category code");
        MaxLength(x => x.PartType, 100, "Part type");
        MaxLength(x => x.FormCategory, 50, "Form category");
        MaxLength(x => x.PurchaseUomCode, 10, "Purchase UOM");
        MaxLength(x => x.SellingUomCode, 10, "Selling UOM");
        MaxLength(x => x.MaterialType, 50, "Material type");
        MaxLength(x => x.SeriesCode, 50, "Series code");
        MaxLength(x => x.PartRevisionNo, 10, "Part revision number");
        MaxLength(x => x.SourceCode, 50, "Source code");
        MaxLength(x => x.RevisionRemark, 500, "Revision remark");
        MaxLength(x => x.HoldRemark, 500, "Hold remark");
        MaxLength(x => x.InactiveRemark, 500, "Inactive remark");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(0m, 9_999_999.9999m)
            .WithMessage("Weight must be between 0 and 9,999,999.9999 kg.")
            .When(x => x.WeightKg.HasValue);

        RuleFor(x => x.MinimumStockLevel)
            .InclusiveBetween(0m, 9_999_999.9999m)
            .WithMessage("Minimum stock level must be between 0 and 9,999,999.9999.")
            .When(x => x.MinimumStockLevel.HasValue);

        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Lead time cannot be negative.")
            .When(x => x.LeadTimeDays.HasValue);

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Reorder point cannot be negative.")
            .When(x => x.ReorderPoint.HasValue);
    }

    private void MaxLength(
        Expression<Func<PartAttributesDto, string?>> selector,
        int maximum,
        string label)
    {
        var read = selector.Compile();

        RuleFor(selector)
            .Must(value => value!.Trim().Length <= maximum)
            .WithMessage($"{label} must be {maximum} characters or fewer.")
            .When(dto => !string.IsNullOrWhiteSpace(read(dto)));
    }
}
