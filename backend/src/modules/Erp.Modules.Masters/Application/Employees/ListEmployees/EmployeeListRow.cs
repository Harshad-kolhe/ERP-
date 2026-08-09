using Erp.Persistence.Domain.Common;

namespace Erp.Modules.Masters.Application.Employees.ListEmployees;

/// <summary>
/// The shape the database query projects into.
/// <para>
/// The name parts stay separate here so <c>firstName</c> and <c>lastName</c> remain
/// individually sortable against the index; they are joined into
/// <c>EmployeeListItemDto.FullName</c> after materialisation.
/// </para>
/// <para>
/// The pay fields are present on the row but reach the DTO only when the caller
/// holds the payroll permission — see <c>ListEmployeesHandler</c>. There is no
/// password field, here or anywhere downstream.
/// </para>
/// </summary>
internal sealed record EmployeeListRow
{
    public required int Id { get; init; }

    public required int? EmployeeCode { get; init; }

    public required string? FirstName { get; init; }

    public required string? MiddleName { get; init; }

    public required string? LastName { get; init; }

    public required string? Gender { get; init; }

    public required string? Address { get; init; }

    public required string? UserName { get; init; }

    /// <summary>Resolved from the legacy role master by the same query.</summary>
    public required string? RoleName { get; init; }

    public required string? Department { get; init; }

    public required string? Designation { get; init; }

    public required string? Email { get; init; }

    public required string? PhoneNo { get; init; }

    public required DateTimeOffset? DateOfBirth { get; init; }

    public required DateTimeOffset? JoiningDate { get; init; }

    public required bool IsMarried { get; init; }

    public required string? BloodGroup { get; init; }

    public required int? ShoeSize { get; init; }

    public required string? AadharNo { get; init; }

    public required string? PanNo { get; init; }

    public required string? PassportNo { get; init; }

    public required string? Qualification { get; init; }

    public required List<string> Skills { get; init; }

    public required List<string> Strengths { get; init; }

    public required bool? IsOverTimeApplicable { get; init; }

    public required bool? WillingToTravel { get; init; }

    public required bool? ApplicableForService { get; init; }

    public required string? BusinessUnit { get; init; }

    public required decimal? ProvidentFund { get; init; }

    public required decimal? EmployeeStateInsurance { get; init; }

    public required decimal? ProfessionalTax { get; init; }

    public required decimal? IncomeTaxTds { get; init; }

    public required decimal? GrossSalary { get; init; }

    public required decimal? NetSalary { get; init; }

    public required decimal? PerHourSalary { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatus Status { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}
