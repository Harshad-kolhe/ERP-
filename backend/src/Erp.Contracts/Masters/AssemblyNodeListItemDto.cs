namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the Section, Assembly or Sub-assembly grid.
/// <para>
/// One DTO for all three screens: they are the same record at three depths, so
/// three near-identical types would differ only in which fields happened to be
/// filled in — and would drift the first time a column was added to one of them.
/// The grids differ in their column list and their permission, which is where the
/// difference actually is.
/// </para>
/// </summary>
public sealed record AssemblyNodeListItemDto
{
    public required Guid Id { get; init; }

    /// <summary>Legacy <c>AssemblyCode</c> — the business key, unique per business unit.</summary>
    public required string Code { get; init; }

    /// <summary>Legacy <c>AssemblyName</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Legacy <c>ManualCode</c> — the code a person assigned, distinct from <see cref="Code"/>.</summary>
    public string? ManualCode { get; init; }

    public required AssemblyLevelDto Level { get; init; }

    /// <summary>Null for a section, which is the top of the breakdown.</summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Parent's code and name, resolved server-side.
    /// <para>
    /// Joined rather than sent as an id the browser then looks up: the legacy grid
    /// showed the parent code and the new one has to as well, and a per-row lookup
    /// is how a 50-row page becomes 51 requests.
    /// </para>
    /// </summary>
    public string? ParentCode { get; init; }

    public string? ParentName { get; init; }

    /// <summary>
    /// How many nodes sit directly under this one. Counted in the same query.
    /// Always 0 for a sub-assembly, which is the bottom of the breakdown.
    /// </summary>
    public required int ChildCount { get; init; }

    public string? MachineType { get; init; }

    /// <summary>Legacy <c>DrivenBy</c> — what powers the assembly (motor, hydraulic, manual).</summary>
    public string? DrivenBy { get; init; }

    /// <summary>Legacy <c>DrawingPath</c>.</summary>
    public string? DrawingPath { get; init; }

    public string? TechnicalSpecification { get; init; }

    public string? Remark { get; init; }

    /// <summary>How many of this node the parent carries. Legacy <c>Quantity</c>.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Kilograms. Legacy <c>TotalWeight</c>.</summary>
    public decimal? WeightKg { get; init; }

    /// <summary>
    /// Legacy <c>SrNo</c> — the order the node appears in on drawings and reports.
    /// Not the grid's row number, which is computed by the browser from the page.
    /// </summary>
    public int? DisplaySequence { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>Display name of the author, resolved server-side. Null if that user no longer exists.</summary>
    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public string? ModifiedBy { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
