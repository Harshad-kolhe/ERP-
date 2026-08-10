using Erp.Api.Common.Security;
using Erp.Api.Features.Employees;
using Erp.Api.Persistence;
using Erp.Contracts.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.UnitTests.Querying;

/// <summary>
/// Redacting the pay columns is only half the boundary. A caller without
/// <c>masters.employee.payroll.read</c> must also be unable to <em>order</em> by
/// them: <c>sort=netSalary:desc</c> against a redacted page still says who earns
/// most, without ever showing a figure. The query map is what stops it, so it is
/// pinned here rather than left to the reader of the projection.
/// </summary>
public sealed class EmployeePayrollQueryMapTests
{
    public static TheoryData<string> PayrollFields =>
    [
        "providentFund",
        "employeeStateInsurance",
        "professionalTax",
        "incomeTaxTds",
        "grossSalary",
        "netSalary",
        "perHourSalary",
    ];

    [Theory]
    [MemberData(nameof(PayrollFields))]
    public async Task Sorting_on_a_pay_column_is_rejected_without_the_payroll_permission(string field)
    {
        using var db = NewContext();
        var queries = new EmployeesQueries(db, new StubUser(canReadPayroll: false));

        var result = await queries.ListAsync(new PageRequest { Sort = $"{field}:desc" }, default);

        result.IsFailure.ShouldBeTrue($"'{field}' must not be sortable without the payroll permission.");
        result.Error.Code.ShouldBe("query.sort.unknown_field");
    }

    [Theory]
    [MemberData(nameof(PayrollFields))]
    public async Task Filtering_on_a_pay_column_is_rejected_without_the_payroll_permission(string field)
    {
        using var db = NewContext();
        var queries = new EmployeesQueries(db, new StubUser(canReadPayroll: false));

        var result = await queries.ListAsync(new PageRequest { Filter = $"{field}:gt:1" }, default);

        result.IsFailure.ShouldBeTrue($"'{field}' must not be filterable without the payroll permission.");
        result.Error.Code.ShouldBe("query.filter.unknown_field");
    }

    /// <summary>
    /// The counterpart: with the permission the field resolves and the query is
    /// translated, so it reaches a database that is not there. Without this the
    /// test above would still pass if the pay columns were dropped from both maps.
    /// </summary>
    [Theory]
    [MemberData(nameof(PayrollFields))]
    public async Task Sorting_on_a_pay_column_is_allowed_with_the_payroll_permission(string field)
    {
        using var db = NewContext();
        var queries = new EmployeesQueries(db, new StubUser(canReadPayroll: true));

        var thrown = await Record.ExceptionAsync(() =>
            queries.ListAsync(new PageRequest { Sort = $"{field}:desc" }, default));

        thrown.ShouldNotBeNull($"The {field} query unexpectedly succeeded without a database.");
        thrown.ShouldBeOfType<SqlException>(
            $"Sorting by {field} did not translate to SQL: {thrown.Message}");
    }

    private static ErpDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlServer(
                "Server=127.0.0.1,1;Database=none;User Id=none;Password=none;"
                + "Encrypt=False;Connect Timeout=1",
                sql => sql.EnableRetryOnFailure(0))
            .Options;

        return new ErpDbContext(options, new SingleUnitContext());
    }

    private sealed class StubUser(bool canReadPayroll) : ICurrentUser
    {
        public Guid UserId => Guid.Empty;

        public string UserName => "test";

        public bool IsAuthenticated => true;

        public IReadOnlySet<string> Permissions =>
            canReadPayroll
                ? new HashSet<string>(StringComparer.Ordinal) { MastersPermissions.EmployeePayrollRead }
                : new HashSet<string>(StringComparer.Ordinal);

        public bool IsSuperAdministrator => false;

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }

    private sealed class SingleUnitContext : IBusinessUnitContext
    {
        public int BusinessUnitId => 1;

        public bool CanAccessAllBusinessUnits => false;
    }
}
