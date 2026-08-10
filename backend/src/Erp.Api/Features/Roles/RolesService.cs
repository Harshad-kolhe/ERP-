using Erp.Api.Common.Results;
using Erp.Api.Domain.Roles;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Roles;

public static class RoleMasterMapping
{
    public static void Apply(Role role, SaveRoleMasterRequest request)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(request);

        role.RolesName = Normalize.Text(request.RolesName);
        role.BypassBusinessUnit = request.BypassBusinessUnit;
        role.IsActive = request.IsActive;
    }

    public static RoleMasterDetailDto ToDetail(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleMasterDetailDto
        {
            Id = role.Id,
            RoleId = role.RoleId,
            RolesName = role.RolesName,
            BypassBusinessUnit = role.BypassBusinessUnit,
            IsActive = role.IsActive,
            CreatedAtUtc = role.CreatedAtUtc,
            ModifiedAtUtc = role.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(role.RowVersion),
        };
    }
}

public sealed class RolesService(ErpDbContext db)
{
    public async Task<Result<int>> CreateAsync(
        CreateRoleMasterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.RolesName.Trim();

        var exists = await db.MasterRoles
            .AsNoTracking()
            .AnyAsync(r => r.RolesName == name, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("role", "name", name));
        }

        var role = new Role { RoleId = request.RoleId };
        RoleMasterMapping.Apply(role, request);

        db.MasterRoles.Add(role);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("role", "name", name));
        }

        return Result.Success(role.Id);
    }

    public async Task<Result> UpdateAsync(
        int id,
        UpdateRoleMasterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MasterWrite.TryDecodeRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure(MasterErrors.StaleRowVersion("role"));
        }

        var role = await db.MasterRoles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role is null)
        {
            return Result.Failure(MasterErrors.NotFound("role", id));
        }

        db.Entry(role).Property(r => r.RowVersion).OriginalValue = rowVersion;

        RoleMasterMapping.Apply(role, request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(MasterErrors.StaleRowVersion("role"));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result.Failure(MasterErrors.DuplicateCode("role", "name", request.RolesName.Trim()));
        }

        return Result.Success();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
