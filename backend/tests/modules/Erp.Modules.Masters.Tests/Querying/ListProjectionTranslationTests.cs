using Erp.BuildingBlocks.Application.Abstractions;
using Erp.Contracts.Common;
using Erp.Modules.Masters.Application.BusinessUnits.ListBusinessUnits;
using Erp.Modules.Masters.Application.Customers.ListCustomers;
using Erp.Modules.Masters.Application.Parts.ListParts;
using Erp.Modules.Masters.Application.Roles.ListRoles;
using Erp.Modules.Masters.Application.Suppliers.ListSuppliers;
using Erp.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Tests.Querying;

/// <summary>
/// The list handlers project straight into the contract DTO, so sorting and
/// filtering are expressed over the projected shape — including the cast to the
/// DTO's status enum and, on parts, the unwrapping of a strongly-typed id.
/// <para>
/// Those are the two things a projection can get wrong in a way the compiler
/// cannot see: EF Core has to translate them into SQL, and when it cannot it
/// throws only once the endpoint is called. Translation happens while the query
/// is compiled, before a connection is opened — so reaching a connection failure
/// is proof the whole query translated. The connection string points nowhere on
/// purpose.
/// </para>
/// <para>
/// Not in the integration suite because no database is needed, and because a
/// translation regression should fail in seconds rather than behind a container.
/// </para>
/// </summary>
public sealed class ListProjectionTranslationTests
{
    /// <summary>
    /// Sorts and filters on <c>status</c> — the enum the projection casts — and
    /// searches, so the generated predicate spans all three code paths.
    /// </summary>
    private static readonly PageRequest StatusPage = new()
    {
        Sort = "status:desc",
        Filter = "status:eq:Draft",
        Search = "acme",
    };

    public static TheoryData<string, Func<ErpDbContext, Task>> Lists => new()
    {
        {
            "suppliers",
            db => new ListSuppliersHandler(db).HandleAsync(new ListSuppliersQuery(StatusPage), default)
        },
        {
            "customers",
            db => new ListCustomersHandler(db).HandleAsync(new ListCustomersQuery(StatusPage), default)
        },
        {
            // Parts keeps an intermediate row type, and this is why: PartId is a
            // value object behind a converter, and EF cannot translate an ORDER BY
            // over the unwrapped `Id.Value` the DTO exposes. The row type carries
            // PartId itself, so the tie-breaker orders by the converted column.
            // Assemblies and parent parts are the same shape for the same reason.
            "parts",
            db => new ListPartsHandler(db).HandleAsync(new ListPartsQuery(StatusPage), default)
        },
        {
            "roles",
            db => new ListRolesHandler(db).HandleAsync(
                new ListRolesQuery(new PageRequest { Sort = "rolesName:asc", Search = "admin" }), default)
        },
        {
            "business units",
            db => new ListBusinessUnitsHandler(db).HandleAsync(
                new ListBusinessUnitsQuery(new PageRequest { Sort = "businessName:asc", Search = "plant" }), default)
        },
    };

    [Theory]
    [MemberData(nameof(Lists))]
    public async Task List_query_translates_to_sql(string list, Func<ErpDbContext, Task> run)
    {
        using var db = NewContext();

        var thrown = await Record.ExceptionAsync(() => run(db));

        // A SqlException means EF got as far as dialling a server that is not there,
        // which it only does once the whole query has been translated. Anything else
        // — in practice InvalidOperationException, "could not be translated" — means
        // the projection, the sort or the filter has no SQL equivalent.
        thrown.ShouldNotBeNull($"The {list} query unexpectedly succeeded without a database.");
        thrown.ShouldBeOfType<SqlException>(
            $"The {list} query did not translate to SQL: {thrown.Message}");
    }

    /// <summary>
    /// The real SQL Server provider, because translation is provider-specific:
    /// proving it against Sqlite or the in-memory provider would prove nothing.
    /// </summary>
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

    private sealed class SingleUnitContext : IBusinessUnitContext
    {
        public int BusinessUnitId => 1;

        public bool CanAccessAllBusinessUnits => false;
    }
}
