namespace Erp.Modules.Masters.Application.BusinessUnits.ListBusinessUnits;

/// <summary>The shape the database query projects into. See <c>SupplierListRow</c> for why it exists.</summary>
internal sealed record BusinessUnitListRow
{
    public required int Id { get; init; }

    public required int? BusinessUnitId { get; init; }

    public required string? BusinessName { get; init; }

    public required string? Address { get; init; }

    public required string? ContactNumber { get; init; }

    public required string? Email { get; init; }

    public required string? Website { get; init; }

    public required string? Cin { get; init; }

    public required string? Gstn { get; init; }

    public required string? StateName { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
