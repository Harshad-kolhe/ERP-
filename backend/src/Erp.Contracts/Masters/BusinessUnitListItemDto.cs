namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the business units grid.
/// <para>
/// This table defines the tenancy boundary rather than sitting inside one, so it
/// is not filtered by the caller's business unit — the read permission is the only
/// thing standing between a caller and the full list.
/// </para>
/// </summary>
public sealed record BusinessUnitListItemDto
{
    public required int Id { get; init; }

    /// <summary>The value other tables carry in their tenancy column.</summary>
    public required int? BusinessUnitId { get; init; }

    public required string? BusinessName { get; init; }

    public string? Address { get; init; }

    public required string? ContactNumber { get; init; }

    public required string? Email { get; init; }

    public string? Website { get; init; }

    /// <summary>Corporate Identification Number.</summary>
    public string? Cin { get; init; }

    public required string? Gstn { get; init; }

    public required string? StateName { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
