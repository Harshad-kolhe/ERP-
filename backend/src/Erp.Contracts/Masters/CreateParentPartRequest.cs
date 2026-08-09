namespace Erp.Contracts.Masters;

/// <summary>
/// Payload for creating a parent part together with its component lines.
/// </summary>
public sealed record CreateParentPartRequest
{
    /// <summary>
    /// The part being built. It must already exist in the part master — the legacy
    /// screen stored part <em>numbers</em> as free text on both sides of the
    /// relationship, so a typo produced a build whose parent no part master row
    /// matched.
    /// </summary>
    public required Guid PartId { get; init; }

    /// <summary>Legacy <c>AssemblyDesc</c>. Optional; the part's own description is shown when this is blank.</summary>
    public string? Description { get; init; }

    /// <summary>Optional link to the section/assembly/sub-assembly this build belongs to.</summary>
    public Guid? AssemblyNodeId { get; init; }

    public string? UnitOfMeasureCode { get; init; }

    public string? DrawingNumber { get; init; }

    public string? Category { get; init; }

    /// <summary>
    /// The component lines, in the order they should appear. May be empty — a
    /// parent part is often raised before its build is worked out — but a part may
    /// not appear on it twice, and may not be a component of itself.
    /// </summary>
    public required IReadOnlyList<ParentPartComponentDto> Components { get; init; }
}
