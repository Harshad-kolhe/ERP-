using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Roles;

public sealed class RolesQueries(ErpDbContext db)
{
    private static readonly QueryMap<RoleMasterListItemDto> Map = QueryMap<RoleMasterListItemDto>.Create()
        .Field("rolesName", x => x.RolesName, searchable: true)
        .Field("roleId", x => x.RoleId)
        .Field("isActive", x => x.IsActive)
        .Field("bypassBusinessUnit", x => x.BypassBusinessUnit)
        .Field("createdAt", x => x.CreatedAtUtc)
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public Task<Result<PagedResult<RoleMasterListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.MasterRoles
            .AsNoTracking()
            .Select(r => new RoleMasterListItemDto
            {
                Id = r.Id,
                RolesName = r.RolesName,
                RoleId = r.RoleId,
                IsActive = r.IsActive,
                BypassBusinessUnit = r.BypassBusinessUnit,
                CreatedAtUtc = r.CreatedAtUtc,
            });

        return rows.ToPagedResultAsync(Map, request, cancellationToken);
    }

    public async Task<Result<RoleMasterDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var role = await db.MasterRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return role is null
            ? Result.Failure<RoleMasterDetailDto>(MasterErrors.NotFound("role", id))
            : Result.Success(RoleMasterMapping.ToDetail(role));
    }
}
