using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Persistence;
using Erp.Persistence.Domain.UnitsOfMeasure;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.UnitsOfMeasure.WriteUnitOfMeasure;

internal sealed record GetUnitOfMeasureByIdQuery(int Id);

internal sealed record CreateUnitOfMeasureCommand(CreateUnitOfMeasureRequest Request);

internal sealed record UpdateUnitOfMeasureCommand(int Id, UpdateUnitOfMeasureRequest Request);

internal static class UnitOfMeasureMapping
{
    public static void Apply(UnitOfMeasure unit, SaveUnitOfMeasureRequest request)
    {
        unit.Name = request.Name.Trim();
        unit.Decimals = request.Decimals;
        unit.BaseUnitCode = Normalize.Code(request.BaseUnitCode);
        unit.ConversionToBase = unit.BaseUnitCode is null ? null : request.ConversionToBase;
        unit.SortOrder = request.SortOrder;
        unit.IsActive = request.IsActive;
    }

    public static UnitOfMeasureDetailDto ToDetail(UnitOfMeasure unit) => new()
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

/// <summary>
/// The rules about a unit's base that only the database can answer.
/// <para>
/// Shared by create and update because a unit can be given a bad base either way,
/// and a rule enforced on one path is not a rule.
/// </para>
/// </summary>
internal static class UnitOfMeasureBase
{
    public static async Task<Error?> ValidateAsync(
        ErpDbContext db,
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

        // One level, deliberately. UnitOfMeasure.BaseCode reads BaseUnitCode ?? Code
        // without following a chain, so a unit pointing at a unit that itself
        // converts would report the wrong family and multiply by the wrong factor.
        // Rejecting the chain here is what keeps that simple reading correct.
        if (baseUnit.BaseUnitCode is not null)
        {
            return Error.Validation(
                "uom.base.not-a-base",
                $"'{baseUnitCode}' itself converts to another unit. Point at the base of the family instead.");
        }

        return null;
    }
}

internal sealed class GetUnitOfMeasureByIdHandler(ErpDbContext db)
    : IQueryHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDetailDto>
{
    public async Task<Result<UnitOfMeasureDetailDto>> HandleAsync(
        GetUnitOfMeasureByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var unit = await db.UnitsOfMeasure
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken);

        return unit is null
            ? Result.Failure<UnitOfMeasureDetailDto>(MasterErrors.NotFound("unit of measure", query.Id))
            : Result.Success(UnitOfMeasureMapping.ToDetail(unit));
    }
}

internal sealed class CreateUnitOfMeasureHandler(ErpDbContext db)
    : ICommandHandler<CreateUnitOfMeasureCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = Normalize.RequiredCode(command.Request.Code);

        var exists = await db.UnitsOfMeasure
            .AsNoTracking()
            .AnyAsync(unit => unit.Code == code, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("unit of measure", "code", code));
        }

        var invalidBase = await UnitOfMeasureBase.ValidateAsync(db, code, command.Request, cancellationToken);

        if (invalidBase is not null)
        {
            return Result.Failure<int>(invalidBase);
        }

        var unit = new UnitOfMeasure { Code = code };
        UnitOfMeasureMapping.Apply(unit, command.Request);

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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

internal sealed class UpdateUnitOfMeasureHandler(ErpDbContext db)
    : ICommandHandler<UpdateUnitOfMeasureCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateUnitOfMeasureCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("unit of measure"));
        }

        var unit = await db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

        if (unit is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("unit of measure", command.Id));
        }

        var invalidBase = await UnitOfMeasureBase.ValidateAsync(db, unit.Code, command.Request, cancellationToken);

        if (invalidBase is not null)
        {
            return Result.Failure<Unit>(invalidBase);
        }

        db.Entry(unit).Property(u => u.RowVersion).OriginalValue = rowVersion;

        // The code is not applied: parts store the letters, so renaming KG would
        // leave every part measured in a unit that no longer exists.
        UnitOfMeasureMapping.Apply(unit, command.Request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("unit of measure"));
        }

        return Result.Success(Unit.Value);
    }
}
