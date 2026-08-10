using Erp.Api.Common.Results;
using Erp.Api.Domain.Suppliers;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Suppliers;

public sealed class SuppliersService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = Normalize.RequiredCode(request.SupplierCode);

        var exists = await db.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.SupplierCode == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("supplier", "code", code));
        }

        var supplier = new Supplier { SupplierCode = code, ProgramId = "SUPPLIER" };
        SupplierMapping.Apply(supplier, request);

        db.Suppliers.Add(supplier);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("supplier", "code", code));
        }

        return Result.Success(supplier.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("supplier"));
        }

        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure(MasterErrors.NotFound("supplier", id));
        }

        db.Entry(supplier).Property(s => s.RowVersion).OriginalValue = rowVersion;

        SupplierMapping.Apply(supplier, request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("supplier"));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
