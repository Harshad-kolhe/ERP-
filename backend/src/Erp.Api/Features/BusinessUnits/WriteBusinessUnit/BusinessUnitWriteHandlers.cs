using System.Globalization;
using Erp.Api.Common.Cqrs;
using Erp.Contracts.Masters;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Domain.BusinessUnits;
using Erp.Api.Common.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.BusinessUnits.WriteBusinessUnit;

public sealed record GetBusinessUnitByIdQuery(int Id);

public sealed record CreateBusinessUnitCommand(CreateBusinessUnitRequest Request);

public sealed record UpdateBusinessUnitCommand(int Id, UpdateBusinessUnitRequest Request);

/// <summary>The one place a business unit's editable fields move between the wire and the entity.</summary>
public static class BusinessUnitMapping
{
    public static void Apply(BusinessUnit unit, SaveBusinessUnitRequest request)
    {
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

    public static BusinessUnitDetailDto ToDetail(BusinessUnit unit) => new()
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

public sealed class GetBusinessUnitByIdHandler(ErpDbContext db)
    : IQueryHandler<GetBusinessUnitByIdQuery, BusinessUnitDetailDto>
{
    public async Task<Result<BusinessUnitDetailDto>> HandleAsync(
        GetBusinessUnitByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var unit = await db.BusinessUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == query.Id, cancellationToken);

        return unit is null
            ? Result.Failure<BusinessUnitDetailDto>(MasterErrors.NotFound("business unit", query.Id))
            : Result.Success(BusinessUnitMapping.ToDetail(unit));
    }
}

public sealed class CreateBusinessUnitHandler(ErpDbContext db)
    : ICommandHandler<CreateBusinessUnitCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateBusinessUnitCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Request.BusinessName.Trim();
        var unitId = command.Request.BusinessUnitId;

        // Both keys checked. A duplicate unit id would silently merge two tenants'
        // data, which is the worst outcome this system has.
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
        BusinessUnitMapping.Apply(unit, command.Request);

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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed class UpdateBusinessUnitHandler(ErpDbContext db)
    : ICommandHandler<UpdateBusinessUnitCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateBusinessUnitCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("business unit"));
        }

        var unit = await db.BusinessUnits.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

        if (unit is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("business unit", command.Id));
        }

        db.Entry(unit).Property(b => b.RowVersion).OriginalValue = rowVersion;

        BusinessUnitMapping.Apply(unit, command.Request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("business unit"));
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result.Failure<Unit>(MasterErrors.DuplicateCode(
                "business unit",
                "name",
                command.Request.BusinessName.Trim()));
        }

        return Result.Success(Unit.Value);
    }
}
