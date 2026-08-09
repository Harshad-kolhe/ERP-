namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the employees grid.
/// <para>
/// Carries no credential field. The legacy grid had a <c>Password</c> column
/// showing the stored value in clear text to anyone who could open the screen;
/// nothing here reproduces it, and nothing ever will — sign-in runs on Identity's
/// PBKDF2 hash, which cannot be displayed even if someone wanted to.
/// </para>
/// <para>
/// The pay fields are nulled unless the caller holds
/// <c>masters.employee.payroll.read</c>. See <c>ListEmployeesHandler</c>: it also
/// withholds them from the sort and filter map, so their ordering cannot be probed
/// by someone forbidden their values.
/// </para>
/// </summary>
public sealed record EmployeeListItemDto
{
    public required int Id { get; init; }

    /// <summary>Rendered as <c>PPE/&lt;code&gt;</c>, which is how everyone refers to it.</summary>
    public required int? EmployeeCode { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    /// <summary>First, middle and last joined on the server so the grid does not re-implement it per screen.</summary>
    public required string FullName { get; init; }

    /// <summary>Legacy code: <c>01</c> male, <c>02</c> female. Labelled by the client.</summary>
    public string? Gender { get; init; }

    public string? Address { get; init; }

    public string? UserName { get; init; }

    /// <summary>
    /// Resolved from the legacy role master in the same query. This is not the
    /// Identity role that grants permissions — see <c>RoleListItemDto</c>.
    /// </summary>
    public string? RoleName { get; init; }

    public required string? Department { get; init; }

    public required string? Designation { get; init; }

    public required string? Email { get; init; }

    public required string? PhoneNo { get; init; }

    public DateTimeOffset? DateOfBirth { get; init; }

    public required DateTimeOffset? JoiningDate { get; init; }

    public bool IsMarried { get; init; }

    public string? BloodGroup { get; init; }

    public int? ShoeSize { get; init; }

    public string? AadharNo { get; init; }

    public string? PanNo { get; init; }

    public string? PassportNo { get; init; }

    public string? Qualification { get; init; }

    public IReadOnlyList<string> Skills { get; init; } = [];

    public IReadOnlyList<string> Strengths { get; init; } = [];

    public bool? IsOverTimeApplicable { get; init; }

    public bool? WillingToTravel { get; init; }

    public bool? ApplicableForService { get; init; }

    /// <summary>Name of the business unit, resolved in the same query rather than by a second call.</summary>
    public string? BusinessUnit { get; init; }

    /// <summary>Provident fund. Null unless the caller holds <c>masters.employee.payroll.read</c>.</summary>
    public decimal? ProvidentFund { get; init; }

    /// <summary>Employee state insurance. Payroll-gated.</summary>
    public decimal? EmployeeStateInsurance { get; init; }

    /// <summary>Professional tax. Payroll-gated.</summary>
    public decimal? ProfessionalTax { get; init; }

    /// <summary>Income tax deducted at source. Payroll-gated.</summary>
    public decimal? IncomeTaxTds { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? GrossSalary { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? NetSalary { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? PerHourSalary { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatusDto Status { get; init; }

    /// <summary>Display name of the author, resolved server-side. Null if that user no longer exists.</summary>
    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public string? ModifiedBy { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
