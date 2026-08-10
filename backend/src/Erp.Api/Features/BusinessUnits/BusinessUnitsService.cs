using System.Globalization;
using Erp.Api.Common.Results;
using Erp.Api.Domain.BusinessUnits;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.BusinessUnits;

public static class BusinessUnitMapping
{
    public static void Apply(BusinessUnit unit, SaveBusinessUnitRequest request)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(request);

        unit.BusinessName = Normalize.Text(request.BusinessName);
        unit.Address = Normalize.Text(request.Address);
        unit.StateName = Normalize.Text(request.StateName);
        unit.StateCode = Normalize.Code(request.StateCode);
        unit.ContactNumber = Normalize.Text(request.ContactNumber);
        unit.Email = Normalize.Text(request.Email);
        unit.Website = Normalize.Text(request.Website);
        unit.Cin = Normalize.Code(request.Cin);
        unit.Gstn = Normalize.Code(request.Gstn);
        unit.Pan = Normalize.Code(request.Pan);
        unit.IsActive = request.IsActive;
    }

    public static BusinessUnitDetailDto ToDetail(BusinessUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        return new BusinessUnitDetailDto
        {
            Id = unit.Id,
            BusinessUnitId = unit.BusinessUnitId,
            BusinessName = unit.BusinessName,
            Address = unit.Address,
            StateName = unit.StateName,
            StateCode = unit.StateCode,
            ContactNumber = unit.ContactNumber,
            Email = unit.Email,
            Website = unit.Website,
            Cin = unit.Cin,
            Gstn = unit.Gstn,
            Pan = unit.Pan,
            IsActive = unit.IsActive,
            CreatedAtUtc = unit.CreatedAtUtc,
            ModifiedAtUtc = unit.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(unit.RowVersion),
        };
    }
}

public sealed class BusinessUnitsService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateBusinessUnitRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.BusinessName.Trim();
        var unitId = request.BusinessUnitId;

        var clash = await db.BusinessUnits
            .AsNoTracking()
            .Where(b => b.BusinessName == name || b.BusinessUnitId == unitId)
            .Select(b => new { b.BusinessName, b.BusinessUnitId })
            .FirstOrDefaultAsync(cancellationToken);

        if (clash is not null)
        {
            return Result.Failure<int>(
                string.Equals(clash.BusinessName, name, StringComparison.OrdinalIgnoreCase)
                    ? MasterErrors.DuplicateCode("business unit", "name", name)
                    : MasterErrors.DuplicateCode(
                        "business unit",
                        "unit id",
                        unitId.ToString(CultureInfo.InvariantCulture)));
        }

        var unit = new BusinessUnit { BusinessUnitId = unitId };
        BusinessUnitMapping.Apply(unit, request);

        db.BusinessUnits.Add(unit);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("business unit", "name", name));
        }

        return Result.Success(unit.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateBusinessUnitRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("business unit"));
        }

        var unit = await db.BusinessUnits.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (unit is null)
        {
            return Result.Failure(MasterErrors.NotFound("business unit", id));
        }

        db.Entry(unit).Property(b => b.RowVersion).OriginalValue = rowVersion;

        BusinessUnitMapping.Apply(unit, request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("business unit"));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure(MasterErrors.DuplicateCode(
                "business unit",
                "name",
                request.BusinessName.Trim()));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
