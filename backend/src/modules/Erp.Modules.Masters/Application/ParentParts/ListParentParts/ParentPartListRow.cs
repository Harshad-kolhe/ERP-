using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Domain.ParentParts;
using Erp.Modules.Masters.Domain.Parts;

namespace Erp.Modules.Masters.Application.ParentParts.ListParentParts;

/// <summary>
/// The shape the database query projects into, before it becomes a contract DTO.
/// Keeps the strongly-typed id inside the query, where EF can translate it.
/// </summary>
internal sealed record ParentPartListRow
{
    public required ParentPartId Id { get; init; }

    public required PartId PartId { get; init; }

    public required string PartNumber { get; init; }

    public required string PartDescription { get; init; }

    public required string? Description { get; init; }

    public required AssemblyNodeId? AssemblyNodeId { get; init; }

    public required string? AssemblyCode { get; init; }

    public required string? AssemblyName { get; init; }

    public required string? UnitOfMeasureCode { get; init; }

    public required string? DrawingNumber { get; init; }

    public required string? Category { get; init; }

    public required int ComponentCount { get; init; }

    public required decimal TotalWeightKg { get; init; }

    public required decimal TotalAmount { get; init; }

    public required bool IsActive { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}
