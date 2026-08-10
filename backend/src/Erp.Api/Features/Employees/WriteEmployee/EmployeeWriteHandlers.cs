using Erp.Api.Common.Security;
using Erp.Api.Common.Cqrs;
using Erp.Contracts.Masters;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Domain.Employees;
using Erp.Api.Common.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Employees.WriteEmployee;

public sealed record GetEmployeeByIdQuery(int Id);

public sealed record CreateEmployeeCommand(CreateEmployeeRequest Request);

public sealed record UpdateEmployeeCommand(int Id, UpdateEmployeeRequest Request);

public sealed class GetEmployeeByIdHandler(ErpDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeDetailDto>
{
    public async Task<Result<EmployeeDetailDto>> HandleAsync(
        GetEmployeeByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<EmployeeDetailDto>(MasterErrors.NotFound("employee", query.Id));
        }

        var canReadPayroll = currentUser.HasPermission(MastersPermissions.EmployeePayrollRead);

        return Result.Success(EmployeeMapping.ToDetail(employee, canReadPayroll));
    }
}

public sealed class CreateEmployeeHandler(ErpDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreateEmployeeCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = command.Request.EmployeeCode;

        var exists = await db.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeCode == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode(
                "employee",
                "code",
                code.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var employee = new Employee { EmployeeCode = code };

        EmployeeMapping.Apply(
            employee,
            command.Request,
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
                code.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return Result.Success(employee.Id);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed class UpdateEmployeeHandler(ErpDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateEmployeeCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("employee"));
        }

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("employee", command.Id));
        }

        db.Entry(employee).Property(e => e.RowVersion).OriginalValue = rowVersion;

        EmployeeMapping.Apply(
            employee,
            command.Request,
            currentUser.HasPermission(MastersPermissions.EmployeePayrollRead));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("employee"));
        }

        return Result.Success(Unit.Value);
    }
}
