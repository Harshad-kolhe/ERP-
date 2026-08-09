namespace Erp.Contracts.Masters;

/// <summary>
/// A single section, assembly or sub-assembly, as returned by the detail endpoint.
/// </summary>
public sealed record AssemblyNodeDetailDto
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? ManualCode { get; init; }

    public required AssemblyLevelDto Level { get; init; }

    public Guid? ParentId { get; init; }

    /// <summary>
    /// Sent alongside <see cref="ParentId"/> so the edit form can label its parent
    /// picker without a second request. The picker searches the server for anything
    /// else.
    /// </summary>
    public string? ParentCode { get; init; }

    public string? ParentName { get; init; }

    public required AssemblyNodeAttributesDto Attributes { get; init; }

    public required bool IsActive { get; init; }

    public required int BusinessUnitId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    /// <summary>
    /// Base64 <c>rowversion</c>. The client must send this back on update; a stale
    /// value produces HTTP 409 instead of silently overwriting a concurrent edit.
    /// </summary>
    public required string RowVersion { get; init; }
}
