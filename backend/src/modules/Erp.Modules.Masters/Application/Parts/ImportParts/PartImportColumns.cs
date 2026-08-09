using Erp.BuildingBlocks.Excel;

namespace Erp.Modules.Masters.Application.Parts.ImportParts;

/// <summary>
/// Every column of the parts import sheet.
/// <para>
/// The headings match the Part Master grid's captions, so an operator who exports
/// the grid and an operator who downloads the template are looking at the same
/// words. Lengths mirror <c>PartConfiguration</c> exactly — that is what lets a
/// too-long value be rejected by name rather than as a SQL truncation error.
/// </para>
/// </summary>
internal static class PartImportColumns
{
    public static readonly ImportColumn PartNumber = new(
        "System part number",
        Required: true,
        MaxLength: 50,
        Note: "The part's business key. Letters, digits, dot, underscore, slash and hyphen only. Must not already exist.");

    public static readonly ImportColumn OriginalPartNumber = new(
        "Original part number",
        MaxLength: 50,
        Note: "For a revision, the number the part family was first issued under. Leave blank for a new part and it copies the system part number.");

    public static readonly ImportColumn ItemNumber = new(
        "Item code (manual)",
        MaxLength: 50);

    public static readonly ImportColumn Description = new(
        "Part description",
        Required: true,
        MaxLength: 250);

    public static readonly ImportColumn TechnicalSpecification = new(
        "Technical specification",
        MaxLength: 2000);

    public static readonly ImportColumn Moc = new("MOC", MaxLength: 50, Note: "Material of construction.");

    public static readonly ImportColumn PartCategoryCode = new("Part category code", MaxLength: 50);

    public static readonly ImportColumn PartType = new("Part type", MaxLength: 100);

    public static readonly ImportColumn FormCategory = new("Form category", MaxLength: 50);

    public static readonly ImportColumn UnitOfMeasureCode = new(
        "Primary UOM",
        Required: true,
        MaxLength: 10,
        Note: "Stored upper-case, so 'nos' and 'NOS' are the same unit.");

    public static readonly ImportColumn PurchaseUomCode = new("Purchase UOM", MaxLength: 10);

    public static readonly ImportColumn SellingUomCode = new("Selling UOM", MaxLength: 10);

    public static readonly ImportColumn MaterialType = new("Material type", MaxLength: 50);

    public static readonly ImportColumn SeriesCode = new("Series code", MaxLength: 50);

    public static readonly ImportColumn PartRevisionNo = new(
        "Part revision no",
        MaxLength: 10,
        Note: "Two digits, e.g. 00 for the first issue.");

    public static readonly ImportColumn SourceCode = new(
        "Source code",
        MaxLength: 50,
        Note: "In House or OutSource.");

    public static readonly ImportColumn WeightKg = new(
        "Weight (kg)",
        ImportColumnKind.Number,
        Note: "Up to four decimal places.");

    public static readonly ImportColumn LeadTimeDays = new("Lead time (days)", ImportColumnKind.WholeNumber);

    public static readonly ImportColumn MinimumStockLevel = new("Minimum stock level", ImportColumnKind.Number);

    public static readonly ImportColumn ReorderPoint = new("Reorder point", ImportColumnKind.WholeNumber);

    public static readonly ImportColumn HsnCode = new(
        "HSN code",
        MaxLength: 10,
        Note: "4, 6 or 8 digits.");

    public static readonly ImportColumn DrawingNumber = new("Drawing revision path", MaxLength: 50);

    public static readonly ImportColumn RevisionRemark = new("Revision remark", MaxLength: 500);

    public static readonly ImportColumn HoldRemark = new("Hold remark", MaxLength: 500);

    public static readonly ImportColumn InactiveRemark = new("Inactive remark", MaxLength: 500);

    public static readonly ImportColumn IsActive = new(
        "Active",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as Yes.");

    public static readonly ImportColumn Status = new(
        "Status",
        Note: "Draft, PendingApproval, Approved, Rejected or Hold. Blank counts as Draft.");

    /// <summary>Sheet order. The template writes them in this order and the grid shows them in it.</summary>
    public static readonly IReadOnlyList<ImportColumn> All =
    [
        PartNumber,
        OriginalPartNumber,
        ItemNumber,
        Description,
        TechnicalSpecification,
        Moc,
        PartCategoryCode,
        PartType,
        FormCategory,
        UnitOfMeasureCode,
        PurchaseUomCode,
        SellingUomCode,
        MaterialType,
        SeriesCode,
        PartRevisionNo,
        SourceCode,
        WeightKg,
        LeadTimeDays,
        MinimumStockLevel,
        ReorderPoint,
        HsnCode,
        DrawingNumber,
        RevisionRemark,
        HoldRemark,
        InactiveRemark,
        IsActive,
        Status,
    ];
}
