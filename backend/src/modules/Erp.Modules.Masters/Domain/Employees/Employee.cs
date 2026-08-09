using Erp.Modules.Masters.Domain.Common;
using Erp.SharedKernel.Primitives;

namespace Erp.Modules.Masters.Domain.Employees;

/// <summary>
/// An employee master record, ported field-for-field from the legacy
/// <c>Employee</c> table.
/// <para>
/// An employee is a <em>person on the payroll</em>, not a login. Authentication runs
/// on <c>ErpUser</c> (ASP.NET Core Identity, PBKDF2-hashed). The credential columns
/// below exist only because the legacy row carries them and the migrated data will
/// contain them — see the warning on <see cref="Password"/>.
/// </para>
/// </summary>
internal sealed class Employee : AggregateRoot<int>, IAuditable, ISoftDeletable, IBusinessUnitScoped, IHasRowVersion
{
    /// <summary>Business key. Unique per business unit.</summary>
    public int? EmployeeCode { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Gender { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }

    public string? Address { get; set; }

    public string? State { get; set; }

    public string? Email { get; set; }

    public string? PhoneNo { get; set; }

    public bool IsMarried { get; set; }

    public string? UserName { get; set; }

    /// <summary>
    /// <strong>Stored in clear text, deliberately and temporarily.</strong> Carried over
    /// so the legacy rows migrate without loss; it is hashed in a follow-up before this
    /// reaches a real environment.
    /// <para>
    /// Nothing authenticates against this column. Sign-in goes through Identity's
    /// PBKDF2 hash on <c>ErpUser</c>, and this value is excluded from every list and
    /// detail DTO so it does not reach the wire.
    /// </para>
    /// </summary>
    public string? Password { get; set; }

    public int? SiteId { get; set; }

    /// <summary>Points at the legacy <c>Role</c> master, not at an Identity role.</summary>
    public int? RoleId { get; set; }

    public string? Department { get; set; }

    public string? Designation { get; set; }

    public DateTimeOffset? JoiningDate { get; set; }

    public string? Qualification { get; set; }

    public string? BloodGroup { get; set; }

    public int? ShoeSize { get; set; }

    public bool? WillingToTravel { get; set; }

    public bool? ApplicableForService { get; set; }

    public bool? IsOverTimeApplicable { get; set; }

    public string? AadharNo { get; set; }

    public string? PanNo { get; set; }

    public string? PassportNo { get; set; }

    /// <summary>Mapped as a primitive collection, so a skill list needs no join table.</summary>
    public List<string> Skill { get; set; } = [];

    public List<string> Strength { get; set; } = [];

    public decimal? ProvidentFund { get; set; }

    public decimal? EmployeeStateInsurance { get; set; }

    public decimal? ProfessionalTax { get; set; }

    public decimal? IncomeTaxTds { get; set; }

    public decimal? GrossSalary { get; set; }

    public decimal? NetSalary { get; set; }

    public decimal? PerHourSalary { get; set; }

    public string? UserEmpCode { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Legacy display ordinal. Carried over; the grid does not read it.</summary>
    public int? SrNo { get; set; }

    public MasterStatus Status { get; set; } = MasterStatus.Draft;

    /// <summary>Replaces the legacy <c>BusinessUnit</c> column, which held the same value.</summary>
    public int BusinessUnitId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
