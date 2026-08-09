using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Persistence;
using Erp.Persistence.Domain.Roles;
using Erp.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Roles.WriteRole;

internal sealed record GetRoleMasterByIdQuery(int Id);

internal sealed record CreateRoleMasterCommand(CreateRoleMasterRequest Request);

internal sealed record UpdateRoleMasterCommand(int Id, UpdateRoleMasterRequest Request);

internal static class RoleMasterMapping
{
    public static void Apply(Role role, SaveRoleMasterRequest request)
    {
        role.RolesName = Normalize.Text(request.RolesName);
        role.BypassBusinessUnit = request.BypassBusinessUnit;
        role.IsActive = request.IsActive;
    }

    public static RoleMasterDetailDto ToDetail(Role role) => new()
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

internal sealed class GetRoleMasterByIdHandler(ErpDbContext db)
    : IQueryHandler<GetRoleMasterByIdQuery, RoleMasterDetailDto>
{
    public async Task<Result<RoleMasterDetailDto>> HandleAsync(
        GetRoleMasterByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var role = await db.MasterRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        return role is null
            ? Result.Failure<RoleMasterDetailDto>(MasterErrors.NotFound("role", query.Id))
            : Result.Success(RoleMasterMapping.ToDetail(role));
    }
}

internal sealed class CreateRoleMasterHandler(ErpDbContext db)
    : ICommandHandler<CreateRoleMasterCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        CreateRoleMasterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Request.RolesName.Trim();

        // Unique system-wide, not per tenant: a role is a cross-tenant concept here.
        var exists = await db.MasterRoles
            .AsNoTracking()
            .AnyAsync(r => r.RolesName == name, cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(MasterErrors.DuplicateCode("role", "name", name));
        }

        var role = new Role { RoleId = command.Request.RoleId };
        RoleMasterMapping.Apply(role, command.Request);

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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

internal sealed class UpdateRoleMasterHandler(ErpDbContext db)
    : ICommandHandler<UpdateRoleMasterCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(
        UpdateRoleMasterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MasterWrite.TryDecodeRowVersion(command.Request.RowVersion, out var rowVersion))
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("role"));
        }

        var role = await db.MasterRoles.FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (role is null)
        {
            return Result.Failure<Unit>(MasterErrors.NotFound("role", command.Id));
        }

        db.Entry(role).Property(r => r.RowVersion).OriginalValue = rowVersion;

        RoleMasterMapping.Apply(role, command.Request);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Unit>(MasterErrors.StaleRowVersion("role"));
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result.Failure<Unit>(MasterErrors.DuplicateCode(
                "role",
                "name",
                command.Request.RolesName.Trim()));
        }

        return Result.Success(Unit.Value);
    }
}
