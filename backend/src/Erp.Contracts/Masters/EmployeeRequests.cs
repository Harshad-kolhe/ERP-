namespace Erp.Contracts.Masters;

/// <summary>
/// The editable fields of an employee.
/// <para>
/// There is no password field, and there will not be. Sign-in runs on Identity's
/// PBKDF2 hash; the legacy screen's clear-text credential column is not reachable
/// from any endpoint here.
/// </para>
/// <para>
/// There is no business unit field either. Tenancy comes from the signed-in user
/// and is stamped by an interceptor, so a request that could name a different one
/// would be a way to write across the boundary.
/// </para>
/// <para>
/// The pay fields are honoured only for callers holding
/// <c>masters.employee.payroll.read</c>. For everyone else they are ignored rather
/// than rejected — an editor without payroll rights saving the contact details of
/// an employee must not blank their salary as a side effect, which is exactly what
/// a form that posts every field it can see would otherwise do.
/// </para>
/// </summary>
public record SaveEmployeeRequest
{
    public required string FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? LastName { get; init; }

    public string? Gender { get; init; }

    public string? Address { get; init; }

    public string? State { get; init; }

    public string? UserName { get; init; }

    /// <summary>Points at the legacy role master, not at an Identity role.</summary>
    public int? RoleId { get; init; }

    public string? Department { get; init; }

    public string? Designation { get; init; }

    public string? Email { get; init; }

    public string? PhoneNo { get; init; }

    public DateTimeOffset? DateOfBirth { get; init; }

    public DateTimeOffset? JoiningDate { get; init; }

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

    /// <summary>Payroll-gated. Ignored without <c>masters.employee.payroll.read</c>.</summary>
    public decimal? ProvidentFund { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? EmployeeStateInsurance { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? ProfessionalTax { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? IncomeTaxTds { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? GrossSalary { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? NetSalary { get; init; }

    /// <summary>Payroll-gated.</summary>
    public decimal? PerHourSalary { get; init; }

    public bool IsActive { get; init; } = true;

    public MasterStatusDto Status { get; init; } = MasterStatusDto.Draft;
}

public sealed record CreateEmployeeRequest : SaveEmployeeRequest
{
    /// <summary>Business key. Digits only — the <c>PPE/</c> prefix is display, not data.</summary>
    public required int EmployeeCode { get; init; }
}

public sealed record UpdateEmployeeRequest : SaveEmployeeRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

/// <summary>One employee, as the edit screen loads it. Pay fields are null without the payroll permission.</summary>
public sealed record EmployeeDetailDto
{
    public required int Id { get; init; }

    public required int? EmployeeCode { get; init; }

    public required string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? LastName { get; init; }

    public string? Gender { get; init; }

    public string? Address { get; init; }

    public string? State { get; init; }

    public string? UserName { get; init; }

    public int? RoleId { get; init; }

    public string? Department { get; init; }

    public string? Designation { get; init; }

    public string? Email { get; init; }

    public string? PhoneNo { get; init; }

    public DateTimeOffset? DateOfBirth { get; init; }

    public DateTimeOffset? JoiningDate { get; init; }

    public required bool IsMarried { get; init; }

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

    public decimal? ProvidentFund { get; init; }

    public decimal? EmployeeStateInsurance { get; init; }

    public decimal? ProfessionalTax { get; init; }

    public decimal? IncomeTaxTds { get; init; }

    public decimal? GrossSalary { get; init; }

    public decimal? NetSalary { get; init; }

    public decimal? PerHourSalary { get; init; }

    /// <summary>
    /// Whether the caller may see and change the pay fields. The form uses it to
    /// decide whether to show that section at all, rather than guessing from nulls
    /// — an employee with no salary recorded and an employee whose salary is hidden
    /// look identical otherwise.
    /// </summary>
    public required bool CanEditPayroll { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatusDto Status { get; init; }

    public required int BusinessUnitId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}
