using Erp.BuildingBlocks.Application.Abstractions;
using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Integration;
using Erp.Persistence;
using Erp.Persistence.Domain.Employees;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Employees.WriteEmployee;

internal sealed record GetEmployeeByIdQuery(int Id);

internal sealed record CreateEmployeeCommand(CreateEmployeeRequest Request);

internal sealed record UpdateEmployeeCommand(int Id, UpdateEmployeeRequest Request);

internal sealed class GetEmployeeByIdHandler(ErpDbContext db, ICurrentUser currentUser)
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

internal sealed class CreateEmployeeHandler(ErpDbContext db, ICurrentUser currentUser)
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

internal sealed class UpdateEmployeeHandler(ErpDbContext db, ICurrentUser currentUser)
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
