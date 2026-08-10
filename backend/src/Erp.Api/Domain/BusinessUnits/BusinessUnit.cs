using Erp.Api.Common.Entities;

namespace Erp.Api.Domain.BusinessUnits;

/// <summary>
/// A business unit â€” the tenancy dimension every other record is scoped to.
/// Ported field-for-field from the legacy <c>BusinessUnit</c>.
/// <para>
/// Deliberately <em>not</em> <see cref="IBusinessUnitScoped"/>, and this one is not a
/// judgement call like <c>Role</c>: scoping this table would make each row visible
/// only from inside the tenant it defines, so an administrator could never see the
/// list they are administering, and a new unit would be invisible to everyone the
/// moment it was created. The tenancy filter is applied to records that *belong to*
/// a business unit; this is the business unit.
/// </para>
/// <para>
/// Access is therefore governed entirely by <c>masters.businessunit.read</c> rather
/// than by the query filter. That is the whole protection on this table, which is
/// why the permission is not granted alongside the ordinary master read rights.
/// </para>
/// </summary>
public sealed class BusinessUnit : AggregateRoot<int>, IAuditable, ISoftDeletable, IHasRowVersion
{
    /// <summary>
    /// Legacy grouping key, distinct from <see cref="Entity{TId}.Id"/>. Kept because
    /// migrated rows across every other table carry this value in their
    /// <c>BusinessUnitId</c> column.
    /// </summary>
    public int? BusinessUnitId { get; set; }

    public string? BusinessName { get; set; }

    public string? Address { get; set; }

    public string? ContactNumber { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    /// <summary>Corporate Identification Number.</summary>
    public string? Cin { get; set; }

    public string? Gstn { get; set; }

    public string? Pan { get; set; }

    public string? StateCode { get; set; }

    public string? StateName { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Legacy display ordinal. Carried over; the grid does not read it.</summary>
    public int? SrNo { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
