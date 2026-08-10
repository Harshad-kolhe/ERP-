using System.Globalization;
using Erp.Api.Common.Results;
using Erp.Api.Common.Security;
using Erp.Api.Domain.Employees;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Employees;

public sealed class EmployeesService(ErpDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<int>> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = request.EmployeeCode;

        var exists = await db.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeCode == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode(
                "employee",
                "code",
                code.ToString(CultureInfo.InvariantCulture)));
        }

        var employee = new Employee { EmployeeCode = code };

        EmployeeMapping.Apply(
            employee,
            request,
            currentUser.HasPermission(MastersPermissions.EmployeePayrollRead));

        db.Employees.Add(employee);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode(
                "employee",
                "code",
                code.ToString(CultureInfo.InvariantCulture)));
        }

        return Result.Success(employee.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("employee"));
        }

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(MasterErrors.NotFound("employee", id));
        }

        db.Entry(employee).Property(e => e.RowVersion).OriginalValue = rowVersion;

        EmployeeMapping.Apply(
            employee,
            request,
            currentUser.HasPermission(MastersPermissions.EmployeePayrollRead));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("employee"));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
