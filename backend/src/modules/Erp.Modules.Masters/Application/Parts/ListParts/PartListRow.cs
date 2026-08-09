using Erp.Modules.Masters.Domain.Parts;

namespace Erp.Modules.Masters.Application.Parts.ListParts;

/// <summary>
/// The shape the database query projects into, before it becomes a contract DTO.
/// <para>
/// It exists so that sorting and filtering happen against the <em>domain</em>
/// <see cref="PartStatus"/> — which EF knows how to translate through its value
/// converter — while <c>Erp.Contracts</c> stays free of any domain type. The rows
/// are mapped to DTOs in memory afterwards, which costs one pass over at most
/// <c>PageRequest.MaxPageSize</c> items.
/// </para>
/// </summary>
internal sealed record PartListRow
{
    /// <summary>
    /// The strongly-typed id, not its <see cref="PartId.Value"/>.
    /// <para>
    /// EF's value converter maps <see cref="PartId"/> to a <c>uniqueidentifier</c>
    /// column, but <c>.Value</c> is a member of the CLR struct and not part of the
    /// mapping — writing <c>p.Id.Value</c> in a projection makes the whole query
    /// untranslatable. The id is unwrapped after materialisation instead.
    /// </para>
    /// </summary>
    public required PartId Id { get; init; }

    public required string PartNumber { get; init; }

    public required string OriginalPartNumber { get; init; }

    public required string? ItemNumber { get; init; }

    public required string Description { get; init; }

    public required string? TechnicalSpecification { get; init; }

    public required string? Moc { get; init; }

    public required string? PartCategoryCode { get; init; }

    public required string? PartType { get; init; }

    public required string? FormCategory { get; init; }

    public required string UnitOfMeasureCode { get; init; }

    public required string? PurchaseUomCode { get; init; }

    public required string? SellingUomCode { get; init; }

    public required string? MaterialType { get; init; }

    public required string? SeriesCode { get; init; }

    public required string? PartRevisionNo { get; init; }

    public required string? SourceCode { get; init; }

    public required decimal? WeightKg { get; init; }

    public required int? LeadTimeDays { get; init; }

    public required decimal? MinimumStockLevel { get; init; }

    public required int? ReorderPoint { get; init; }

    public required string? HsnCode { get; init; }

    public required string? DrawingNumber { get; init; }

    public required bool IsActive { get; init; }

    public required PartStatus Status { get; init; }

    public required string? RevisionRemark { get; init; }

    public required string? HoldRemark { get; init; }

    public required string? InactiveRemark { get; init; }

    /// <summary>Left-joined from the audit-user view, so it is null for a deleted author.</summary>
    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}
