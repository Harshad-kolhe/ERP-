using Erp.BuildingBlocks.Excel;

namespace Erp.Modules.Masters.Application.Employees.ImportEmployees;

/// <summary>
/// Every column of the employees import sheet.
/// <para>
/// Two legacy columns are absent and stay absent. <c>Password</c> is not importable
/// at all — credentials belong to Identity and are hashed, and a column that
/// accepted plain text would recreate the exact hazard this rewrite removed. The
/// legacy <c>BusinessUnit</c> column is also gone: the tenancy of an imported row
/// comes from the signed-in user, stamped by the interceptor, so letting a sheet
/// name a different business unit would be a way to write into another tenant.
/// </para>
/// <para>
/// The pay columns <em>are</em> importable, because a migration has to carry them,
/// but the endpoint is gated on <c>masters.employee.import</c> rather than the
/// ordinary create right.
/// </para>
/// </summary>
internal static class EmployeeImportColumns
{
    public static readonly ImportColumn EmployeeCode = new(
        "Employee code",
        ImportColumnKind.WholeNumber,
        Required: true,
        Note: "Digits only — enter 1043, not PPE/1043. Must not already exist.");

    public static readonly ImportColumn FirstName = new("First name", Required: true, MaxLength: 100);

    public static readonly ImportColumn MiddleName = new("Middle name", MaxLength: 100);

    public static readonly ImportColumn LastName = new("Last name", MaxLength: 100);

    public static readonly ImportColumn Gender = new(
        "Gender",
        MaxLength: 20,
        Note: "Male, Female, or the legacy codes 01 and 02.");

    public static readonly ImportColumn Address = new("Employee address", MaxLength: 500);

    public static readonly ImportColumn State = new("State", MaxLength: 100);

    public static readonly ImportColumn UserName = new("User name", MaxLength: 100);

    public static readonly ImportColumn RoleId = new(
        "Role id",
        ImportColumnKind.WholeNumber,
        Note: "The legacy role master's Role id, as shown on the Roles grid.");

    public static readonly ImportColumn Department = new("Department", MaxLength: 100);

    public static readonly ImportColumn Designation = new("Designation", MaxLength: 100);

    public static readonly ImportColumn Email = new("Email", MaxLength: 150);

    public static readonly ImportColumn PhoneNo = new("Phone no", MaxLength: 30);

    public static readonly ImportColumn DateOfBirth = new("Date of birth", ImportColumnKind.Date);

    public static readonly ImportColumn JoiningDate = new("Date of joining", ImportColumnKind.Date);

    public static readonly ImportColumn IsMarried = new(
        "Married",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as No.");

    public static readonly ImportColumn BloodGroup = new("Blood group", MaxLength: 10);

    public static readonly ImportColumn ShoeSize = new("Shoe size", ImportColumnKind.WholeNumber);

    public static readonly ImportColumn AadharNo = new("Aadhar card no.", MaxLength: 12);

    public static readonly ImportColumn PanNo = new("Pan card no.", MaxLength: 10);

    public static readonly ImportColumn PassportNo = new("Passport no.", MaxLength: 20);

    public static readonly ImportColumn Qualification = new("Qualification", MaxLength: 200);

    public static readonly ImportColumn Skills = new(
        "Skills",
        ImportColumnKind.TextList,
        Note: "Comma-separated in one cell, e.g. Welding, Turning, Assembly.");

    public static readonly ImportColumn Strengths = new("Strength", ImportColumnKind.TextList, Note: "Comma-separated.");

    public static readonly ImportColumn IsOverTimeApplicable = new("Is over time applicable", ImportColumnKind.Boolean);

    public static readonly ImportColumn WillingToTravel = new("Willing to travel", ImportColumnKind.Boolean);

    public static readonly ImportColumn ApplicableForService = new("Applicable for service", ImportColumnKind.Boolean);

    public static readonly ImportColumn ProvidentFund = new("Provident fund (PF)", ImportColumnKind.Number);

    public static readonly ImportColumn EmployeeStateInsurance =
        new("Employee state insurance (ESI)", ImportColumnKind.Number);

    public static readonly ImportColumn ProfessionalTax = new("Professional tax (PT)", ImportColumnKind.Number);

    public static readonly ImportColumn IncomeTaxTds = new("Income tax (TDS)", ImportColumnKind.Number);

    public static readonly ImportColumn GrossSalary = new("Gross salary", ImportColumnKind.Number);

    public static readonly ImportColumn NetSalary = new("Net salary", ImportColumnKind.Number);

    public static readonly ImportColumn PerHourSalary = new("Per hour salary", ImportColumnKind.Number);

    public static readonly ImportColumn IsActive = new(
        "Active",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as Yes.");

    public static readonly ImportColumn Status = new(
        "Status",
        Note: "Draft, PendingApproval, Approved or Inactive. Blank counts as Draft.");

    public static readonly IReadOnlyList<ImportColumn> All =
    [
        EmployeeCode,
        FirstName,
        MiddleName,
        LastName,
        Gender,
        Address,
        State,
        UserName,
        RoleId,
        Department,
        Designation,
        Email,
        PhoneNo,
        DateOfBirth,
        JoiningDate,
        IsMarried,
        BloodGroup,
        ShoeSize,
        AadharNo,
        PanNo,
        PassportNo,
        Qualification,
        Skills,
        Strengths,
        IsOverTimeApplicable,
        WillingToTravel,
        ApplicableForService,
        ProvidentFund,
        EmployeeStateInsurance,
        ProfessionalTax,
        IncomeTaxTds,
        GrossSalary,
        NetSalary,
        PerHourSalary,
        IsActive,
        Status,
    ];
}
