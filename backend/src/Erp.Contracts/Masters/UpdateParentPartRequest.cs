namespace Erp.Contracts.Masters;

/// <summary>
/// Payload for updating a parent part and its component lines.
/// <para>
/// The parent part id is absent: which part this record describes is its identity,
/// and changing it would silently re-point the whole build at a different part.
/// </para>
/// </summary>
public sealed record UpdateParentPartRequest
{
    public string? Description { get; init; }

    public Guid? AssemblyNodeId { get; init; }

    public string? UnitOfMeasureCode { get; init; }

    public string? DrawingNumber { get; init; }

    public string? Category { get; init; }

    /// <summary>
    /// The complete component list as it should end up — not a set of changes.
    /// Lines missing from it are removed, lines present are created or updated, and
    /// the whole replacement happens in one transaction so the totals can never be
    /// observed halfway through.
    /// </summary>
    public required IReadOnlyList<ParentPartComponentDto> Components { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}
