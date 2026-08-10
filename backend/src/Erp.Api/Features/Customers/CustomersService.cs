using Erp.Api.Common.Results;
using Erp.Api.Domain.Customers;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Customers;

public sealed class CustomersService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = Normalize.RequiredCode(request.CustomerCode);

        var exists = await db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.CustomerCode == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("customer", "code", code));
        }

        var customer = new Customer { CustomerCode = code };
        CustomerMapping.Apply(customer, request);

        db.Customers.Add(customer);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("customer", "code", code));
        }

        return Result.Success(customer.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("customer"));
        }

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(MasterErrors.NotFound("customer", id));
        }

        db.Entry(customer).Property(c => c.RowVersion).OriginalValue = rowVersion;

        CustomerMapping.Apply(customer, request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("customer"));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
