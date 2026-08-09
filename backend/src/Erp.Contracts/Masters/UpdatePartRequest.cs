namespace Erp.Contracts.Masters;

/// <summary>
/// Payload for updating a part.
/// <para>
/// The part number is absent by design: it is the business key, and renaming it
/// silently re-points every purchase order, BOM line and stock ledger entry that
/// refers to it. Changing a part number is a separate, audited operation.
/// </para>
/// </summary>
public sealed record UpdatePartRequest
{
    public required string Description { get; init; }

    public Guid? CategoryId { get; init; }

    public required string UnitOfMeasureCode { get; init; }

    public string? HsnCode { get; init; }

    public string? DrawingNumber { get; init; }

    /// <summary>
    /// The descriptive fields, sent whole. This is a replace, not a patch: a field
    /// left out is cleared, because a form that submits everything it shows and a
    /// server that ignores blanks is how a value nobody can delete comes about.
    /// </summary>
    public PartAttributesDto? Attributes { get; init; }

    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}
