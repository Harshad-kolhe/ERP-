namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the parts grid.
/// <para>
/// Wider than a list row usually wants to be, and for a reason: this grid replaces
/// the legacy Part Master, whose users work across every one of these columns, so
/// dropping any of them would make the new screen a downgrade rather than a
/// replacement. Roughly a third arrive hidden and are turned on from the column
/// chooser, exactly as they were before.
/// </para>
/// <para>
/// It is still narrower than <see cref="PartDetailDto"/> and it is still projected
/// straight from SQL — the handler never loads a part aggregate — so the cost is one
/// wider row per page, not one query per column.
/// </para>
/// </summary>
public sealed record PartListItemDto
{
    public required Guid Id { get; init; }

    /// <summary>Legacy "System Part Number" — the number the system issues.</summary>
    public required string PartNumber { get; init; }

    /// <summary>
    /// The number this part's family was first issued under. Equal to
    /// <see cref="PartNumber"/> unless the part is a revision of an earlier one.
    /// </summary>
    public required string OriginalPartNumber { get; init; }

    /// <summary>Legacy "Item Code (Manual)" — the code a person assigned.</summary>
    public string? ItemNumber { get; init; }

    /// <summary>Legacy "Part Description".</summary>
    public required string Description { get; init; }

    /// <summary>Legacy "Technical Specification".</summary>
    public string? TechnicalSpecification { get; init; }

    /// <summary>Material of construction.</summary>
    public string? Moc { get; init; }

    /// <summary>Legacy "Part Cate.code".</summary>
    public string? PartCategoryCode { get; init; }

    public string? PartType { get; init; }

    public string? FormCategory { get; init; }

    /// <summary>Legacy "Primary UOM".</summary>
    public required string UnitOfMeasureCode { get; init; }

    public string? PurchaseUomCode { get; init; }

    public string? SellingUomCode { get; init; }

    public string? MaterialType { get; init; }

    public string? SeriesCode { get; init; }

    public string? PartRevisionNo { get; init; }

    public string? SourceCode { get; init; }

    /// <summary>Kilograms. Sent as a number, formatted by the browser in the viewer's locale.</summary>
    public decimal? WeightKg { get; init; }

    public int? LeadTimeDays { get; init; }

    /// <summary>Legacy <c>SafetyStockLevel</c>, labelled "Minimum Stock Level" on its own form.</summary>
    public decimal? MinimumStockLevel { get; init; }

    public int? ReorderPoint { get; init; }

    public string? HsnCode { get; init; }

    /// <summary>Legacy "Drawing Revision Path".</summary>
    public string? DrawingNumber { get; init; }

    /// <summary>Whether the part may be used on new documents. Independent of <see cref="Status"/>.</summary>
    public required bool IsActive { get; init; }

    public required PartStatusDto Status { get; init; }

    public string? RevisionRemark { get; init; }

    public string? HoldRemark { get; init; }

    public string? InactiveRemark { get; init; }

    /// <summary>
    /// Display name of the author, resolved server-side. Null when the user record
    /// has since been removed, which is why it is not <c>required</c>.
    /// </summary>
    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Legacy spelled this <c>ModifyedBy</c>. The typo is not carried forward.</summary>
    public string? ModifiedBy { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
