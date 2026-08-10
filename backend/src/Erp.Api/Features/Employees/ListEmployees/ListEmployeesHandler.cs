using Erp.Api.Common.Security;
using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Paging;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Api.Persistence;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Employees.ListEmployees;

/// <summary>
/// Returns one page of employees.
/// <para>
/// The projection is a security boundary as much as a performance one. The
/// <c>Password</c> column on the entity is not named by it and never will be, so no
/// combination of <c>sort=</c> and <c>filter=</c> can reach the credential.
/// </para>
/// <para>
/// The pay columns are a softer boundary: they are read, then withheld from callers
/// without <see cref="MastersPermissions.EmployeePayrollRead"/>, and those callers
/// also get a query map with no pay fields in it. Both halves are needed. Redacting
/// alone would leave <c>sort=netSalary:desc</c> ordering the page by a number the
/// caller may not see, which leaks who earns most without showing a figure.
/// </para>
/// </summary>
public sealed class ListEmployeesHandler(ErpDbContext db, ICurrentUser currentUser)
    : IQueryHandler<ListEmployeesQuery, PagedResult<EmployeeListItemDto>>
{
    /// <summary>The fields any reader of the employee master may sort and filter on.</summary>
    private static readonly QueryMap<EmployeeListRow> WithoutPayroll = BaseMap().Build();

    /// <summary>The same, plus the pay columns.</summary>
    private static readonly QueryMap<EmployeeListRow> WithPayroll = BaseMap()
        .Field("providentFund", x => x.ProvidentFund)
        .Field("employeeStateInsurance", x => x.EmployeeStateInsurance)
        .Field("professionalTax", x => x.ProfessionalTax)
        .Field("incomeTaxTds", x => x.IncomeTaxTds)
        .Field("grossSalary", x => x.GrossSalary)
        .Field("netSalary", x => x.NetSalary)
        .Field("perHourSalary", x => x.PerHourSalary)
        .Build();

    public async Task<Result<PagedResult<EmployeeListItemDto>>> HandleAsync(
        ListEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var canReadPayroll = currentUser.HasPermission(MastersPermissions.EmployeePayrollRead);

        // The role and business unit names come from the same query rather than a
        // lookup per row. Both live in this module's own schema, so these are plain
        // correlated subqueries the database plans as joins.
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

        var page = await rows.ToPagedResultAsync(map, query.Page, cancellationToken);

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

    /// <summary>
    /// Everything the two maps share. A method rather than a field so each map owns
    /// its own builder â€” a builder is mutable, and sharing one would let the payroll
    /// fields leak into the map that exists to be without them.
    /// </summary>
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
            // Newest first: a master is worked from the end, and the row somebody
            // just added is the one they came back to check. Any column header
            // still reorders it, and the tie-breaker keeps paging stable either way.
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

    /// <summary>
    /// Joined in memory rather than in SQL: string concatenation of nullable columns
    /// translates to a CASE-heavy expression that cannot use the name index.
    /// </summary>
    private static string FullName(EmployeeListRow row) =>
        string.Join(' ', new[] { row.FirstName, row.MiddleName, row.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
}
