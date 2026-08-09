namespace Erp.Contracts.Masters;

/// <summary>
/// A single part, as returned by the detail endpoint.
/// </summary>
public sealed record PartDetailDto
{
    public required Guid Id { get; init; }

    public required string PartNumber { get; init; }

    public required string Description { get; init; }

    /// <summary>
    /// Set once the Category master lands in Phase 1. Stored now so parts created
    /// today do not need backfilling then.
    /// </summary>
    public Guid? CategoryId { get; init; }

    public required string UnitOfMeasureCode { get; init; }

    /// <summary>HSN code, for GST classification on purchase and dispatch documents.</summary>
    public string? HsnCode { get; init; }

    public string? DrawingNumber { get; init; }

    /// <summary>The descriptive fields, in the same shape the update endpoint accepts back.</summary>
    public required PartAttributesDto Attributes { get; init; }

    /// <summary>Whether the part may be used on new documents. Independent of <see cref="Status"/>.</summary>
    public required bool IsActive { get; init; }

    public required PartStatusDto Status { get; init; }

    public required int BusinessUnitId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    /// <summary>
    /// Base64 <c>rowversion</c>. The client must send this back on update; a stale
    /// value produces HTTP 409 instead of silently overwriting a concurrent edit.
    /// </summary>
    public required string RowVersion { get; init; }
}
