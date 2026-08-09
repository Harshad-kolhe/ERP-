namespace Erp.Contracts.Masters;

/// <summary>
/// The descriptive fields of a part, shared by the create and update payloads and
/// echoed back on the detail response.
/// <para>
/// One type rather than the same twenty properties written three times: the create
/// and edit forms show the same fields, and three copies is three places for them to
/// drift apart. Every member is optional — a part is identified by its number,
/// description and unit of measure, and the rest is filled in as the design settles.
/// </para>
/// </summary>
public sealed record PartAttributesDto
{
    /// <summary>The manually assigned item code. Legacy "Item Code (Manual)".</summary>
    public string? ItemNumber { get; init; }

    public string? TechnicalSpecification { get; init; }

    /// <summary>Material of construction.</summary>
    public string? Moc { get; init; }

    public string? PartCategoryCode { get; init; }

    public string? PartType { get; init; }

    public string? FormCategory { get; init; }

    public string? PurchaseUomCode { get; init; }

    public string? SellingUomCode { get; init; }

    public string? MaterialType { get; init; }

    public string? SeriesCode { get; init; }

    public string? PartRevisionNo { get; init; }

    public string? SourceCode { get; init; }

    /// <summary>Kilograms.</summary>
    public decimal? WeightKg { get; init; }

    /// <summary>Days.</summary>
    public int? LeadTimeDays { get; init; }

    /// <summary>Legacy <c>SafetyStockLevel</c>, labelled "Minimum Stock Level".</summary>
    public decimal? MinimumStockLevel { get; init; }

    public int? ReorderPoint { get; init; }

    public string? RevisionRemark { get; init; }

    public string? HoldRemark { get; init; }

    public string? InactiveRemark { get; init; }
}
