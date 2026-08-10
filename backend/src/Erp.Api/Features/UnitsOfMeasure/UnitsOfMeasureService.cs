using Erp.Api.Common.Results;
using Erp.Api.Domain.UnitsOfMeasure;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.UnitsOfMeasure;

public static class UnitOfMeasureMapping
{
    public static void Apply(UnitOfMeasure unit, SaveUnitOfMeasureRequest request)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(request);

        unit.Name = request.Name.Trim();
        unit.Decimals = request.Decimals;
        unit.BaseUnitCode = Normalize.Code(request.BaseUnitCode);
        unit.ConversionToBase = unit.BaseUnitCode is null ? null : request.ConversionToBase;
        unit.SortOrder = request.SortOrder;
        unit.IsActive = request.IsActive;
    }

    public static UnitOfMeasureDetailDto ToDetail(UnitOfMeasure unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        return new UnitOfMeasureDetailDto
        {
            Id = unit.Id,
            Code = unit.Code,
            Name = unit.Name,
            Decimals = unit.Decimals,
            BaseUnitCode = unit.BaseUnitCode,
            ConversionToBase = unit.ConversionToBase,
            SortOrder = unit.SortOrder,
            IsActive = unit.IsActive,
            CreatedAtUtc = unit.CreatedAtUtc,
            ModifiedAtUtc = unit.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(unit.RowVersion),
        };
    }
}

public sealed class UnitsOfMeasureService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = Normalize.RequiredCode(request.Code);

        var exists = await db.UnitsOfMeasure
            .AsNoTracking()
            .AnyAsync(unit => unit.Code == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("unit of measure", "code", code));
        }

        var invalidBase = await ValidateBaseAsync(code, request, cancellationToken);

        if (invalidBase is not null)
        {
            return Result.Failure<int>(invalidBase);
        }

        var unit = new UnitOfMeasure { Code = code };
        UnitOfMeasureMapping.Apply(unit, request);

        db.UnitsOfMeasure.Add(unit);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("unit of measure", "code", code));
        }

        return Result.Success(unit.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("unit of measure"));
        }

        var unit = await db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit is null)
        {
            return Result.Failure(MasterErrors.NotFound("unit of measure", id));
        }

        var invalidBase = await ValidateBaseAsync(unit.Code, request, cancellationToken);

        if (invalidBase is not null)
        {
            return Result.Failure(invalidBase);
        }

        db.Entry(unit).Property(u => u.RowVersion).OriginalValue = rowVersion;

        UnitOfMeasureMapping.Apply(unit, request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("unit of measure"));
        }

        return Result.Success();
    }

    private async Task<Error?> ValidateBaseAsync(
        string code,
        SaveUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var baseUnitCode = Normalize.Code(request.BaseUnitCode);

        if (baseUnitCode is null)
        {
            return null;
        }

        if (string.Equals(baseUnitCode, code, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(
                "uom.base.self",
                "A unit cannot convert to itself. Leave the base unit blank if this unit is a base.");
        }

        if (request.ConversionToBase is not > 0)
        {
            return Error.Validation(
                "uom.conversion.required",
                "A unit with a base unit needs a conversion factor greater than zero.");
        }

        var baseUnit = await db.UnitsOfMeasure
            .AsNoTracking()
            .Where(unit => unit.Code == baseUnitCode)
            .Select(unit => new { unit.BaseUnitCode })
            .FirstOrDefaultAsync(cancellationToken);

        if (baseUnit is null)
        {
            return Error.Validation(
                "uom.base.unknown",
                $"There is no unit '{baseUnitCode}' to convert to.");
        }

        if (baseUnit.BaseUnitCode is not null)
        {
            return Error.Validation(
                "uom.base.not-a-base",
                $"'{baseUnitCode}' itself converts to another unit. Point at the base of the family instead.");
        }

        return null;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
