using Erp.Api.Common.Results;
using Erp.Api.Domain.Lookups;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.LookupValues;

public static class LookupValueMapping
{
    public static void Apply(LookupValue value, SaveLookupValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(request);

        value.Name = request.Name.Trim();
        value.SortOrder = request.SortOrder;
        value.IsActive = request.IsActive;
    }

    public static LookupValueDetailDto ToDetail(LookupValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new LookupValueDetailDto
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
}

public sealed class LookupValuesService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateLookupValueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var type = request.Type.Trim();
        var code = request.Code.Trim();

        var exists = await db.LookupValues
            .AsNoTracking()
            .AnyAsync(v => v.Type == type && v.Code == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("lookup value", "code", code));
        }

        var value = new LookupValue { Type = type, Code = code };
        LookupValueMapping.Apply(value, request);

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

    public async Task<Result> UpdateAsync(
        int id,
        UpdateLookupValueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("lookup value"));
        }

        var value = await db.LookupValues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (value is null)
        {
            return Result.Failure(MasterErrors.NotFound("lookup value", id));
        }

        db.Entry(value).Property(v => v.RowVersion).OriginalValue = rowVersion;

        LookupValueMapping.Apply(value, request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("lookup value"));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
