namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the Parent Part grid — a part that is built from other parts,
/// together with the totals rolled up from its components.
/// <para>
/// The legacy <c>AssemblyMaster</c> table stored the header and its child lines in
/// the same table, told apart by whether <c>ChildPart</c> was null, and then wrote
/// a second copy of every row into an <c>AssemblyPartMaster</c> table that nothing
/// kept in step. This grid lists headers only; the components come back with the
/// detail.
/// </para>
/// </summary>
public sealed record ParentPartListItemDto
{
    public required Guid Id { get; init; }

    /// <summary>The part this record describes the build of.</summary>
    public required Guid PartId { get; init; }

    /// <summary>Resolved server-side from the part master, so the grid shows a number rather than a Guid.</summary>
    public required string PartNumber { get; init; }

    public required string PartDescription { get; init; }

    /// <summary>Legacy <c>AssemblyDesc</c> — what this build is called, when that differs from the part description.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The assembly node this build belongs to, if any. Legacy <c>AssemblyCode</c>
    /// was free text; here it is a real reference, so the code shown is one that
    /// exists.
    /// </summary>
    public Guid? AssemblyNodeId { get; init; }

    public string? AssemblyCode { get; init; }

    public string? AssemblyName { get; init; }

    public string? UnitOfMeasureCode { get; init; }

    public string? DrawingNumber { get; init; }

    public string? Category { get; init; }

    /// <summary>How many component lines the build has. Counted in the same query.</summary>
    public required int ComponentCount { get; init; }

    /// <summary>
    /// Kilograms, summed from the component lines rather than typed in.
    /// <para>
    /// The legacy screen recalculated this after every child insert and wrote the
    /// answer onto whichever row had the parent's number in its <em>child</em>
    /// column — usually no row at all, so the total silently stayed at whatever it
    /// was first saved as.
    /// </para>
    /// </summary>
    public required decimal TotalWeightKg { get; init; }

    /// <summary>Sum of quantity × rate across the component lines.</summary>
    public required decimal TotalAmount { get; init; }

    public required bool IsActive { get; init; }

    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public string? ModifiedBy { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
