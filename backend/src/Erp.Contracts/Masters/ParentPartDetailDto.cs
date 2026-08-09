namespace Erp.Contracts.Masters;

/// <summary>
/// A parent part and every component line it is built from.
/// <para>
/// Header and lines arrive together and go back together. A parent part with no
/// components is not a useful record, and an endpoint per line would make
/// "replace the whole build in one transaction" — which is what the edit screen
/// does — impossible to express without a batch API on top.
/// </para>
/// </summary>
public sealed record ParentPartDetailDto
{
    public required Guid Id { get; init; }

    public required Guid PartId { get; init; }

    public required string PartNumber { get; init; }

    public required string PartDescription { get; init; }

    public string? Description { get; init; }

    public Guid? AssemblyNodeId { get; init; }

    public string? AssemblyCode { get; init; }

    public string? AssemblyName { get; init; }

    public string? UnitOfMeasureCode { get; init; }

    public string? DrawingNumber { get; init; }

    public string? Category { get; init; }

    /// <summary>In the stored order, which is the order the user arranged them in.</summary>
    public required IReadOnlyList<ParentPartComponentDto> Components { get; init; }

    /// <summary>Summed from <see cref="Components"/> by the server.</summary>
    public required decimal TotalWeightKg { get; init; }

    public required decimal TotalAmount { get; init; }

    public required bool IsActive { get; init; }

    public required int BusinessUnitId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    /// <summary>Base64 <c>rowversion</c>; send it back on update or the write is rejected with 409.</summary>
    public required string RowVersion { get; init; }
}
