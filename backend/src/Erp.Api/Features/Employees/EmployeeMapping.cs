using Erp.Contracts.Masters;
using Erp.Api.Features.Masters;
using Erp.Api.Domain.Common;
using Erp.Api.Domain.Employees;

namespace Erp.Api.Features.Employees;

/// <summary>The one place an employee's editable fields move between the wire and the entity.</summary>
public static class EmployeeMapping
{
    public static void Apply(Employee employee, SaveEmployeeRequest request, bool canWritePayroll)
    {
        employee.FirstName = Normalize.Text(request.FirstName);
        employee.MiddleName = Normalize.Text(request.MiddleName);
        employee.LastName = Normalize.Text(request.LastName);
        employee.Gender = Normalize.Text(request.Gender);
        employee.Address = Normalize.Text(request.Address);
        employee.State = Normalize.Text(request.State);
        employee.UserName = Normalize.Text(request.UserName);
        employee.RoleId = request.RoleId;
        employee.Department = Normalize.Text(request.Department);
        employee.Designation = Normalize.Text(request.Designation);
        employee.Email = Normalize.Text(request.Email);
        employee.PhoneNo = Normalize.Text(request.PhoneNo);
        employee.DateOfBirth = request.DateOfBirth;
        employee.JoiningDate = request.JoiningDate;
        employee.IsMarried = request.IsMarried;
        employee.BloodGroup = Normalize.Code(request.BloodGroup);

        employee.ShoeSize = request.ShoeSize;
        employee.AadharNo = Normalize.Text(request.AadharNo);
        employee.PanNo = Normalize.Code(request.PanNo);
        employee.PassportNo = Normalize.Code(request.PassportNo);
        employee.Qualification = Normalize.Text(request.Qualification);

        // Rebuilt rather than mutated: the request carries the whole list, and
        // merging would make removing a skill impossible.
        employee.Skill = [.. request.Skills.Select(skill => skill.Trim()).Where(skill => skill.Length > 0)];
        employee.Strength = [.. request.Strengths.Select(item => item.Trim()).Where(item => item.Length > 0)];

        employee.IsOverTimeApplicable = request.IsOverTimeApplicable;
        employee.WillingToTravel = request.WillingToTravel;
        employee.ApplicableForService = request.ApplicableForService;

        // Skipped entirely without the permission, so the values already on the row
        // survive an edit made by somebody who could not see them.
        if (canWritePayroll)
        {
            employee.ProvidentFund = request.ProvidentFund;
            employee.EmployeeStateInsurance = request.EmployeeStateInsurance;
            employee.ProfessionalTax = request.ProfessionalTax;
            employee.IncomeTaxTds = request.IncomeTaxTds;
            employee.GrossSalary = request.GrossSalary;
            employee.NetSalary = request.NetSalary;
            employee.PerHourSalary = request.PerHourSalary;
        }

        employee.IsActive = request.IsActive;
        employee.Status = (MasterStatus)request.Status;
    }

    public static EmployeeDetailDto ToDetail(Employee employee, bool canReadPayroll) => new()
    {
        Id = employee.Id,
        EmployeeCode = employee.EmployeeCode,
        FirstName = employee.FirstName,
        MiddleName = employee.MiddleName,
        LastName = employee.LastName,
        Gender = employee.Gender,
        Address = employee.Address,
        State = employee.State,
        UserName = employee.UserName,
        RoleId = employee.RoleId,
        Department = employee.Department,
        Designation = employee.Designation,
        Email = employee.Email,
        PhoneNo = employee.PhoneNo,
        DateOfBirth = employee.DateOfBirth,
        JoiningDate = employee.JoiningDate,
        IsMarried = employee.IsMarried,
        BloodGroup = employee.BloodGroup,
        ShoeSize = employee.ShoeSize,
        AadharNo = employee.AadharNo,
        PanNo = employee.PanNo,
        PassportNo = employee.PassportNo,
        Qualification = employee.Qualification,
        Skills = employee.Skill,
        Strengths = employee.Strength,
        IsOverTimeApplicable = employee.IsOverTimeApplicable,
        WillingToTravel = employee.WillingToTravel,
        ApplicableForService = employee.ApplicableForService,
        ProvidentFund = canReadPayroll ? employee.ProvidentFund : null,
        EmployeeStateInsurance = canReadPayroll ? employee.EmployeeStateInsurance : null,
        ProfessionalTax = canReadPayroll ? employee.ProfessionalTax : null,
        IncomeTaxTds = canReadPayroll ? employee.IncomeTaxTds : null,
        GrossSalary = canReadPayroll ? employee.GrossSalary : null,
        NetSalary = canReadPayroll ? employee.NetSalary : null,
        PerHourSalary = canReadPayroll ? employee.PerHourSalary : null,
        CanEditPayroll = canReadPayroll,
        IsActive = employee.IsActive,
        Status = (MasterStatusDto)employee.Status,
        BusinessUnitId = employee.BusinessUnitId,
        CreatedAtUtc = employee.CreatedAtUtc,
        ModifiedAtUtc = employee.ModifiedAtUtc,
        RowVersion = Convert.ToBase64String(employee.RowVersion),
    };
}
