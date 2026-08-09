using Erp.SharedKernel.Primitives;

namespace Erp.Modules.Masters.Domain.Roles;

/// <summary>
/// A role master record, ported from the legacy <c>Roles</c> table.
/// <para>
/// <strong>This is not the role that grants permissions.</strong> Authorisation runs
/// on ASP.NET Core Identity roles (<c>ErpRole</c>) with permissions attached as role
/// claims, which is what <c>RequirePermission</c> checks on every request. This entity
/// exists because the legacy master screen and its migrated data do, and because
/// <c>Employee.RoleId</c> points at it. The two need reconciling before the roles and
/// permissions administration screen is built — see the note in the module README.
/// </para>
/// <para>
/// Deliberately <em>not</em> <see cref="IBusinessUnitScoped"/>: the legacy row carries
/// <see cref="BypassBusinessUnit"/>, so a role is a cross-tenant concept and a tenancy
/// filter here would hide rows the administration screen has to show.
/// </para>
/// </summary>
internal sealed class Role : AggregateRoot<int>, IAuditable, ISoftDeletable, IHasRowVersion
{
    public string? RolesName { get; set; }

    /// <summary>
    /// Legacy grouping key, distinct from <see cref="Entity{TId}.Id"/>. Kept because
    /// migrated <c>Employee</c> rows reference it.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>Legacy display ordinal. Carried over; the grid does not read it.</summary>
    public int? SrNo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Lets holders of this role read across every business unit.</summary>
    public bool BypassBusinessUnit { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
