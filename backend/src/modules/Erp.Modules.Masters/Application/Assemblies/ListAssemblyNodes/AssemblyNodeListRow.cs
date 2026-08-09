using Erp.Modules.Masters.Domain.Assemblies;

namespace Erp.Modules.Masters.Application.Assemblies.ListAssemblyNodes;

/// <summary>
/// The shape the database query projects into, before it becomes a contract DTO.
/// <para>
/// It exists so sorting and filtering happen against the <em>domain</em>
/// <see cref="AssemblyLevel"/> and <see cref="AssemblyNodeId"/> — which EF knows how
/// to translate through their value converters — while <c>Erp.Contracts</c> stays
/// free of any domain type.
/// </para>
/// </summary>
internal sealed record AssemblyNodeListRow
{
    /// <summary>
    /// The strongly-typed id, not its <c>Value</c>: <c>.Value</c> is a member of the
    /// CLR struct rather than of the mapping, and writing it in a projection makes
    /// the whole query untranslatable.
    /// </summary>
    public required AssemblyNodeId Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string? ManualCode { get; init; }

    public required AssemblyLevel Level { get; init; }

    public required AssemblyNodeId? ParentId { get; init; }

    public required string? ParentCode { get; init; }

    public required string? ParentName { get; init; }

    public required int ChildCount { get; init; }

    public required string? MachineType { get; init; }

    public required string? DrivenBy { get; init; }

    public required string? DrawingPath { get; init; }

    public required string? TechnicalSpecification { get; init; }

    public required string? Remark { get; init; }

    public required decimal? Quantity { get; init; }

    public required decimal? WeightKg { get; init; }

    public required int? DisplaySequence { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>Left-joined from the audit-user view, so it is null for a deleted author.</summary>
    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}
