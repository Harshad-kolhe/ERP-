using Erp.Api.Common.Cqrs;
using Erp.Contracts.Masters;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Domain.Customers;
using Erp.Api.Common.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Customers.WriteCustomer;

public sealed record GetCustomerByIdQuery(int Id);

public sealed record CreateCustomerCommand(CreateCustomerRequest Request);

public sealed record UpdateCustomerCommand(int Id, UpdateCustomerRequest Request);

public sealed class GetCustomerByIdHandler(ErpDbContext db)
    : IQueryHandler<GetCustomerByIdQuery, CustomerDetailDto>
{
    public async Task<Result<CustomerDetailDto>> HandleAsync(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        return customer is null
            ? Result.Failure<CustomerDetailDto>(MasterErrors.NotFound("customer", query.Id))
            : Result.Success(CustomerMapping.ToDetail(customer));
    }
}

public sealed class CreateCustomerHandler(ErpDbContext db)
    : ICommandHandler<CreateCustomerCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = Normalize.RequiredCode(command.Request.CustomerCode);

        var exists = await db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.CustomerCode == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("customer", "code", code));
        }

        var customer = new Customer { CustomerCode = code };
        CustomerMapping.Apply(customer, command.Request);

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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed class UpdateCustomerHandler(ErpDbContext db)
    : ICommandHandler<UpdateCustomerCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("customer"));
        }

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("customer", command.Id));
        }

        db.Entry(customer).Property(c => c.RowVersion).OriginalValue = rowVersion;

        CustomerMapping.Apply(customer, command.Request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("customer"));
        }

        return Result.Success(Unit.Value);
    }
}
