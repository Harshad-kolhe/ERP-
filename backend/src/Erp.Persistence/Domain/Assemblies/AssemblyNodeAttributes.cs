namespace Erp.Persistence.Domain.Assemblies;

/// <summary>
/// The descriptive fields of an assembly node, carried as one value so
/// <see cref="AssemblyNode.Create"/> and <see cref="AssemblyNode.Update"/> take
/// the same set and cannot fall out of step.
/// <para>
/// Mirrors <c>AssemblyNodeAttributesDto</c>. It is a separate type because the
/// contracts assembly may not reference the domain, and because the domain should
/// not be reshaped by a change made for the wire.
/// </para>
/// </summary>
public sealed record AssemblyNodeAttributes
{
    public string? ManualCode { get; init; }

    public string? MachineType { get; init; }

    public string? DrivenBy { get; init; }

    public string? DrawingPath { get; init; }

    public string? TechnicalSpecification { get; init; }

    public string? Remark { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? WeightKg { get; init; }

    public int? DisplaySequence { get; init; }
}
