using System.Linq.Expressions;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Modules.Masters.Application.Parts;

/// <summary>
/// Rules for the part's descriptive fields, shared by create and update.
/// <para>
/// The lengths match the column widths in <c>PartConfiguration</c> exactly. That is
/// the whole point of stating them here: without it, an over-long technical
/// specification reaches SQL Server and comes back as a truncation error with no
/// field name in it, which tells the user nothing about which box to fix.
/// </para>
/// <para>
/// The numeric rules reject negatives rather than clamping them. A weight of −5 kg
/// is a typo, and silently storing 0 hides it until someone costs a machine.
/// </para>
/// </summary>
internal sealed class PartAttributesValidator : AbstractValidator<PartAttributesDto>
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

        // 9,999,999.9999 kg, the widest value the (18,4) column takes without the
        // integer part overflowing what anyone would enter by hand.
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

    /// <summary>
    /// Checks the trimmed length, because that is what the domain stores — otherwise
    /// a value pasted from a spreadsheet with trailing spaces is rejected for being
    /// one character too long when it is not.
    /// </summary>
    private void MaxLength(
        Expression<Func<PartAttributesDto, string?>> selector,
        int maximum,
        string label)
    {
        // Compiled once here, not inside the When: compiling per request would put
        // expression-tree work on every field of every save.
        var read = selector.Compile();

        RuleFor(selector)
            .Must(value => value!.Trim().Length <= maximum)
            .WithMessage($"{label} must be {maximum} characters or fewer.")
            .When(dto => !string.IsNullOrWhiteSpace(read(dto)));
    }
}
