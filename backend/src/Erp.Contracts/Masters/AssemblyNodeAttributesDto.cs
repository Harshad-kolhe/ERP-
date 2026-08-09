namespace Erp.Contracts.Masters;

/// <summary>
/// The descriptive fields of a section, assembly or sub-assembly — everything
/// except its identity, its place in the tree and whether it is in use.
/// <para>
/// Grouped into one type for the same reason <see cref="PartAttributesDto"/> is:
/// create, update and detail all carry exactly this set, so a field added here
/// reaches all three at once instead of being remembered into two of them.
/// </para>
/// </summary>
public sealed record AssemblyNodeAttributesDto
{
    /// <summary>The code a person assigned, kept separate from the system code.</summary>
    public string? ManualCode { get; init; }

    /// <summary>Which machine family this node belongs to. Legacy <c>MachineType</c>.</summary>
    public string? MachineType { get; init; }

    /// <summary>What powers it — motor, hydraulic, pneumatic, manual. Legacy <c>DrivenBy</c>.</summary>
    public string? DrivenBy { get; init; }

    /// <summary>Path to the current drawing. Legacy <c>DrawingPath</c>.</summary>
    public string? DrawingPath { get; init; }

    public string? TechnicalSpecification { get; init; }

    public string? Remark { get; init; }

    /// <summary>How many of this node its parent carries.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Kilograms. Legacy <c>TotalWeight</c>.</summary>
    public decimal? WeightKg { get; init; }

    /// <summary>The order this node appears in on drawings and reports. Legacy <c>SrNo</c>.</summary>
    public int? DisplaySequence { get; init; }
}
