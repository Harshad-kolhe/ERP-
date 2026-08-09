using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Domain.Suppliers;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Suppliers.WriteSupplier;

internal sealed record GetSupplierByIdQuery(int Id);

internal sealed record CreateSupplierCommand(CreateSupplierRequest Request);

internal sealed record UpdateSupplierCommand(int Id, UpdateSupplierRequest Request);

internal sealed class GetSupplierByIdHandler(MastersDbContext db)
    : IQueryHandler<GetSupplierByIdQuery, SupplierDetailDto>
{
    public async Task<Result<SupplierDetailDto>> HandleAsync(
        GetSupplierByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Tracking is off: this is a read, and the edit that follows arrives as a
        // separate request with its own load.
        var supplier = await db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);

        return supplier is null
            ? Result.Failure<SupplierDetailDto>(MasterErrors.NotFound("supplier", query.Id))
            : Result.Success(SupplierMapping.ToDetail(supplier));
    }
}

internal sealed class CreateSupplierHandler(MastersDbContext db)
    : ICommandHandler<CreateSupplierCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = Normalize.RequiredCode(command.Request.SupplierCode);

        // Checked so the user gets a precise message rather than a database error.
        // The unique index is still what guarantees it — see the catch below.
        var exists = await db.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.SupplierCode == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("supplier", "code", code));
        }

        var supplier = new Supplier { SupplierCode = code, ProgramId = "SUPPLIER" };
        SupplierMapping.Apply(supplier, command.Request);

        db.Suppliers.Add(supplier);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Two requests passed the check above concurrently and the index
            // rejected the loser. The constraint is the source of truth, not the read.
            return Result.Failure<int>(MasterErrors.DuplicateCode("supplier", "code", code));
        }

        return Result.Success(supplier.Id);
    }

    /// <summary>SQL Server 2601 (unique index) and 2627 (unique constraint).</summary>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

internal sealed class UpdateSupplierHandler(MastersDbContext db)
    : ICommandHandler<UpdateSupplierCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("supplier"));
        }

        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("supplier", command.Id));
        }

        // Tell EF the version the client was looking at. If the row has moved on,
        // the UPDATE matches zero rows and EF raises rather than silently discarding
        // the other person's edit.
        db.Entry(supplier).Property(s => s.RowVersion).OriginalValue = rowVersion;

        SupplierMapping.Apply(supplier, command.Request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("supplier"));
        }

        return Result.Success(Unit.Value);
    }
}
