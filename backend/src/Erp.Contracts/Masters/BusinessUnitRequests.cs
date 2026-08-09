namespace Erp.Contracts.Masters;

/// <summary>
/// The editable fields of a business unit.
/// <para>
/// No status: this table is not an approvable record but the tenancy boundary
/// itself, and a business unit sitting in "pending approval" would leave every
/// record scoped to it in an undefined state.
/// </para>
/// </summary>
public record SaveBusinessUnitRequest
{
    public required string BusinessName { get; init; }

    public string? Address { get; init; }

    public string? StateName { get; init; }

    public string? StateCode { get; init; }

    public string? ContactNumber { get; init; }

    public string? Email { get; init; }

    public string? Website { get; init; }

    /// <summary>Corporate Identification Number.</summary>
    public string? Cin { get; init; }

    public string? Gstn { get; init; }

    public string? Pan { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateBusinessUnitRequest : SaveBusinessUnitRequest
{
    /// <summary>
    /// The number every other table carries in its tenancy column. Chosen rather
    /// than generated, because migrated data already references it.
    /// </summary>
    public required int BusinessUnitId { get; init; }
}

public sealed record UpdateBusinessUnitRequest : SaveBusinessUnitRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

/// <summary>One business unit, as the edit screen loads it.</summary>
public sealed record BusinessUnitDetailDto
{
    public required int Id { get; init; }

    public required int? BusinessUnitId { get; init; }

    public required string? BusinessName { get; init; }

    public string? Address { get; init; }

    public string? StateName { get; init; }

    public string? StateCode { get; init; }

    public string? ContactNumber { get; init; }

    public string? Email { get; init; }

    public string? Website { get; init; }

    public string? Cin { get; init; }

    public string? Gstn { get; init; }

    public string? Pan { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}

/// <summary>
/// The editable fields of a legacy role master row.
/// <para>
/// This grants nothing. Permissions live on Identity roles and are edited on the
/// roles administration screen; these rows exist so <c>Employee.RoleId</c> has
/// something to point at.
/// </para>
/// </summary>
public record SaveRoleMasterRequest
{
    public required string RolesName { get; init; }

    /// <summary>Lets holders read across every business unit.</summary>
    public bool BypassBusinessUnit { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CreateRoleMasterRequest : SaveRoleMasterRequest
{
    /// <summary>The legacy role number that employee rows reference.</summary>
    public required int RoleId { get; init; }
}

public sealed record UpdateRoleMasterRequest : SaveRoleMasterRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

/// <summary>One legacy role master row, as the edit screen loads it.</summary>
public sealed record RoleMasterDetailDto
{
    public required int Id { get; init; }

    public required int RoleId { get; init; }

    public required string? RolesName { get; init; }

    public required bool BypassBusinessUnit { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}
