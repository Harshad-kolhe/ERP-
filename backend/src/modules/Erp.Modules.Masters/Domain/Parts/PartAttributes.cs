namespace Erp.Modules.Masters.Domain.Parts;

/// <summary>
/// The descriptive attributes a part carries alongside its identity, grouped into
/// one value rather than added as twenty more parameters on
/// <see cref="Part.Create"/> and <see cref="Part.Update"/>.
/// <para>
/// Every member here exists because the legacy Part Master grid shows it. The
/// legacy names are recorded on each property: the columns arrive from that system
/// during migration, and someone reading a migration script needs to be able to
/// map the two without opening a 2,727-line JavaScript file. The names themselves
/// are not carried over — <c>ItemDescription2</c> holds a technical specification,
/// <c>SafetyStockLevel</c> is a minimum, and <c>HSCode</c> is an HSN code.
/// </para>
/// <para>
/// Nothing here participates in a state transition, which is why it is a plain
/// record and not part of the aggregate's rules. Identity (<c>PartNumber</c>),
/// classification (<c>Description</c>, <c>UnitOfMeasureCode</c>) and lifecycle
/// (<c>Status</c>) stay on <see cref="Part"/>, because those are the things the
/// aggregate protects.
/// </para>
/// </summary>
internal sealed record PartAttributes
{
    /// <summary>Legacy <c>ItemNumber</c>. The manually assigned item code, distinct from the system part number.</summary>
    public string? ItemNumber { get; init; }

    /// <summary>
    /// Legacy <c>ItemDescription2</c>. Free text up to 2,000 characters, stored as
    /// Unicode: it carries engineering symbols (Ω, µ, ×, Ø) that a non-Unicode column
    /// silently folds into look-alikes.
    /// </summary>
    public string? TechnicalSpecification { get; init; }

    /// <summary>Material of construction. Legacy <c>MOC</c>.</summary>
    public string? Moc { get; init; }

    /// <summary>Legacy <c>ItemCategoryCode1</c>, shown as "Part Cate.code".</summary>
    public string? PartCategoryCode { get; init; }

    /// <summary>
    /// Legacy <c>ItemCategoryCode3</c>, which the legacy grid displayed as the resolved
    /// text of its category row under the caption "Part Type".
    /// </summary>
    public string? PartType { get; init; }

    /// <summary>Legacy <c>ItemCategoryCode2</c>, shown as "Form Category".</summary>
    public string? FormCategory { get; init; }

    /// <summary>Legacy <c>PurchaseUoM</c>. Off by default in the grid, as it was there.</summary>
    public string? PurchaseUomCode { get; init; }

    /// <summary>Legacy <c>SellingUoM</c>. Off by default in the grid, as it was there.</summary>
    public string? SellingUomCode { get; init; }

    public string? MaterialType { get; init; }

    public string? SeriesCode { get; init; }

    /// <summary>Legacy <c>PartRevisionNo</c> — a two-digit revision label, not a number.</summary>
    public string? PartRevisionNo { get; init; }

    /// <summary>Whether the part is made in house or bought out. Legacy <c>SourceCode</c>.</summary>
    public string? SourceCode { get; init; }

    /// <summary>Legacy <c>Weight</c>, in kilograms.</summary>
    public decimal? WeightKg { get; init; }

    /// <summary>Legacy <c>LeadTime</c>, in days.</summary>
    public int? LeadTimeDays { get; init; }

    /// <summary>
    /// Legacy <c>SafetyStockLevel</c>, which its own form labelled "Minimum Stock
    /// Level". Named for what it means.
    /// </summary>
    public decimal? MinimumStockLevel { get; init; }

    public int? ReorderPoint { get; init; }

    /// <summary>Legacy <c>Remark</c>, shown as "Revision Remark".</summary>
    public string? RevisionRemark { get; init; }

    /// <summary>Why the part was put on hold. Written when the status moves to hold.</summary>
    public string? HoldRemark { get; init; }

    /// <summary>Why the part was deactivated.</summary>
    public string? InactiveRemark { get; init; }
}
