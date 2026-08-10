using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Common.Security;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Employees;

public sealed class EmployeesQueries(ErpDbContext db, ICurrentUser currentUser)
{
    private static readonly QueryMap<EmployeeListRow> WithoutPayroll = BaseMap().Build();

    private static readonly QueryMap<EmployeeListRow> WithPayroll = BaseMap()
        .Field("providentFund", x => x.ProvidentFund)
        .Field("employeeStateInsurance", x => x.EmployeeStateInsurance)
        .Field("professionalTax", x => x.ProfessionalTax)
        .Field("incomeTaxTds", x => x.IncomeTaxTds)
        .Field("grossSalary", x => x.GrossSalary)
        .Field("netSalary", x => x.NetSalary)
        .Field("perHourSalary", x => x.PerHourSalary)
        .Build();

    public async Task<Result<PagedResult<EmployeeListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var canReadPayroll = currentUser.HasPermission(MastersPermissions.EmployeePayrollRead);

        var rows = db.Employees
            .AsNoTracking()
            .Select(e => new EmployeeListRow
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                MiddleName = e.MiddleName,
                LastName = e.LastName,
                Gender = e.Gender,
                Address = e.Address,
                UserName = e.UserName,
                RoleName = db.MasterRoles
                    .Where(r => r.RoleId == e.RoleId)
                    .Select(r => r.RolesName)
                    .FirstOrDefault(),
                Department = e.Department,
                Designation = e.Designation,
                Email = e.Email,
                PhoneNo = e.PhoneNo,
                DateOfBirth = e.DateOfBirth,
                JoiningDate = e.JoiningDate,
                IsMarried = e.IsMarried,
                BloodGroup = e.BloodGroup,
                ShoeSize = e.ShoeSize,
                AadharNo = e.AadharNo,
                PanNo = e.PanNo,
                PassportNo = e.PassportNo,
                Qualification = e.Qualification,
                Skills = e.Skill,
                Strengths = e.Strength,
                IsOverTimeApplicable = e.IsOverTimeApplicable,
                WillingToTravel = e.WillingToTravel,
                ApplicableForService = e.ApplicableForService,
                BusinessUnit = db.BusinessUnits
                    .Where(b => b.BusinessUnitId == e.BusinessUnitId)
                    .Select(b => b.BusinessName)
                    .FirstOrDefault(),
                ProvidentFund = e.ProvidentFund,
                EmployeeStateInsurance = e.EmployeeStateInsurance,
                ProfessionalTax = e.ProfessionalTax,
                IncomeTaxTds = e.IncomeTaxTds,
                GrossSalary = e.GrossSalary,
                NetSalary = e.NetSalary,
                PerHourSalary = e.PerHourSalary,
                IsActive = e.IsActive,
                Status = e.Status,
                CreatedBy = db.Users
                    .Where(u => u.Id == e.CreatedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = e.CreatedAtUtc,
                ModifiedBy = db.Users
                    .Where(u => u.Id == e.ModifiedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = e.ModifiedAtUtc,
            });

        var map = canReadPayroll ? WithPayroll : WithoutPayroll;

        var page = await rows.ToPagedResultAsync(map, request, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<EmployeeListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(row => ToDto(row, canReadPayroll)).ToList();

        return Result.Success(new PagedResult<EmployeeListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    public async Task<Result<EmployeeDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<EmployeeDetailDto>(MasterErrors.NotFound("employee", id));
        }

        var canReadPayroll = currentUser.HasPermission(MastersPermissions.EmployeePayrollRead);

        return Result.Success(EmployeeMapping.ToDetail(employee, canReadPayroll));
    }

    private static QueryMapBuilder<EmployeeListRow> BaseMap() =>
        QueryMap<EmployeeListRow>.Create()
            .Field("employeeCode", x => x.EmployeeCode)
            .Field("firstName", x => x.FirstName, searchable: true)
            .Field("lastName", x => x.LastName, searchable: true)
            .Field("gender", x => x.Gender)
            .Field("address", x => x.Address)
            .Field("userName", x => x.UserName, searchable: true)
            .Field("roleName", x => x.RoleName)
            .Field("department", x => x.Department)
            .Field("designation", x => x.Designation)
            .Field("email", x => x.Email, searchable: true)
            .Field("phoneNo", x => x.PhoneNo)
            .Field("dateOfBirth", x => x.DateOfBirth)
            .Field("joiningDate", x => x.JoiningDate)
            .Field("isMarried", x => x.IsMarried)
            .Field("bloodGroup", x => x.BloodGroup)
            .Field("shoeSize", x => x.ShoeSize)
            .Field("aadharNo", x => x.AadharNo)
            .Field("panNo", x => x.PanNo)
            .Field("passportNo", x => x.PassportNo)
            .Field("qualification", x => x.Qualification)
            .Field("isOverTimeApplicable", x => x.IsOverTimeApplicable)
            .Field("willingToTravel", x => x.WillingToTravel)
            .Field("applicableForService", x => x.ApplicableForService)
            .Field("businessUnit", x => x.BusinessUnit)
            .Field("isActive", x => x.IsActive)
            .Field("status", x => x.Status)
            .Field("createdBy", x => x.CreatedBy)
            .Field("createdAt", x => x.CreatedAtUtc)
            .Field("modifiedBy", x => x.ModifiedBy)
            .Field("modifiedAt", x => x.ModifiedAtUtc)
            .DefaultSort("createdAt", descending: true)
            .TieBreaker(x => x.Id);

    private static EmployeeListItemDto ToDto(EmployeeListRow row, bool canReadPayroll) => new()
    {
        Id = row.Id,
        EmployeeCode = row.EmployeeCode,
        FirstName = row.FirstName,
        LastName = row.LastName,
        FullName = FullName(row),
        Gender = row.Gender,
        Address = row.Address,
        UserName = row.UserName,
        RoleName = row.RoleName,
        Department = row.Department,
        Designation = row.Designation,
        Email = row.Email,
        PhoneNo = row.PhoneNo,
        DateOfBirth = row.DateOfBirth,
        JoiningDate = row.JoiningDate,
        IsMarried = row.IsMarried,
        BloodGroup = row.BloodGroup,
        ShoeSize = row.ShoeSize,
        AadharNo = row.AadharNo,
        PanNo = row.PanNo,
        PassportNo = row.PassportNo,
        Qualification = row.Qualification,
        Skills = row.Skills,
        Strengths = row.Strengths,
        IsOverTimeApplicable = row.IsOverTimeApplicable,
        WillingToTravel = row.WillingToTravel,
        ApplicableForService = row.ApplicableForService,
        BusinessUnit = row.BusinessUnit,
        ProvidentFund = canReadPayroll ? row.ProvidentFund : null,
        EmployeeStateInsurance = canReadPayroll ? row.EmployeeStateInsurance : null,
        ProfessionalTax = canReadPayroll ? row.ProfessionalTax : null,
        IncomeTaxTds = canReadPayroll ? row.IncomeTaxTds : null,
        GrossSalary = canReadPayroll ? row.GrossSalary : null,
        NetSalary = canReadPayroll ? row.NetSalary : null,
        PerHourSalary = canReadPayroll ? row.PerHourSalary : null,
        IsActive = row.IsActive,
        Status = (MasterStatusDto)row.Status,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };

    private static string FullName(EmployeeListRow row) =>
        string.Join(' ', new[] { row.FirstName, row.MiddleName, row.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
}
