using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Persistence;
using Erp.Persistence.Domain.Lookups;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.LookupValues.WriteLookupValue;

internal sealed record GetLookupValueByIdQuery(int Id);

internal sealed record CreateLookupValueCommand(CreateLookupValueRequest Request);

internal sealed record UpdateLookupValueCommand(int Id, UpdateLookupValueRequest Request);

internal static class LookupValueMapping
{
    public static void Apply(LookupValue value, SaveLookupValueRequest request)
    {
        value.Name = request.Name.Trim();
        value.SortOrder = request.SortOrder;
        value.IsActive = request.IsActive;
    }

    public static LookupValueDetailDto ToDetail(LookupValue value) => new()
    {
        Id = value.Id,
        Type = value.Type,
        Code = value.Code,
        Name = value.Name,
        SortOrder = value.SortOrder,
        IsActive = value.IsActive,
        CreatedAtUtc = value.CreatedAtUtc,
        ModifiedAtUtc = value.ModifiedAtUtc,
        RowVersion = Convert.ToBase64String(value.RowVersion),
    };
}

internal sealed class GetLookupValueByIdHandler(ErpDbContext db)
    : IQueryHandler<GetLookupValueByIdQuery, LookupValueDetailDto>
{
    public async Task<Result<LookupValueDetailDto>> HandleAsync(
        GetLookupValueByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var value = await db.LookupValues
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == query.Id, cancellationToken);

        return value is null
            ? Result.Failure<LookupValueDetailDto>(MasterErrors.NotFound("lookup value", query.Id))
            : Result.Success(LookupValueMapping.ToDetail(value));
    }
}

internal sealed class CreateLookupValueHandler(ErpDbContext db)
    : ICommandHandler<CreateLookupValueCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateLookupValueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var type = command.Request.Type.Trim();
        var code = command.Request.Code.Trim();

        // Unique within its list, not across the table: "Assembly" is legitimately
        // both a part type and a form category.
        var exists = await db.LookupValues
            .AsNoTracking()
            .AnyAsync(v => v.Type == type && v.Code == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("lookup value", "code", code));
        }

        var value = new LookupValue { Type = type, Code = code };
        LookupValueMapping.Apply(value, command.Request);

        db.LookupValues.Add(value);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("lookup value", "code", code));
        }

        return Result.Success(value.Id);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

internal sealed class UpdateLookupValueHandler(ErpDbContext db)
    : ICommandHandler<UpdateLookupValueCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateLookupValueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("lookup value"));
        }

        var value = await db.LookupValues.FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

        if (value is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("lookup value", command.Id));
        }

        db.Entry(value).Property(v => v.RowVersion).OriginalValue = rowVersion;

        // Type and Code are not applied. They identify the option, and every record
        // already storing the code would be silently reinterpreted by a change here.
        LookupValueMapping.Apply(value, command.Request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("lookup value"));
        }

        return Result.Success(Unit.Value);
    }
}
