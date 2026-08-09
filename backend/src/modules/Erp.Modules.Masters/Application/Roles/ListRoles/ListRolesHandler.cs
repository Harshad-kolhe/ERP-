using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Roles.ListRoles;

/// <summary>
/// Returns one page of legacy role master rows.
/// <para>
/// Unlike the other masters this set is not business-unit scoped, so the same page
/// is returned to every tenant — which is correct here and deliberate: see
/// <c>Role</c>.
/// </para>
/// </summary>
internal sealed class ListRolesHandler(ErpDbContext db)
    : IQueryHandler<ListRolesQuery, PagedResult<RoleListItemDto>>
{
    private static readonly QueryMap<RoleListRow> Map = QueryMap<RoleListRow>.Create()
        .Field("rolesName", x => x.RolesName, searchable: true)
        .Field("roleId", x => x.RoleId)
        .Field("isActive", x => x.IsActive)
        .Field("bypassBusinessUnit", x => x.BypassBusinessUnit)
        .Field("createdAt", x => x.CreatedAtUtc)
        // Newest first: a master is worked from the end, and the row somebody
        // just added is the one they came back to check. Any column header still
        // reorders it, and the tie-breaker below keeps paging stable either way.
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<RoleListItemDto>>> HandleAsync(
        ListRolesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = db.MasterRoles
            .AsNoTracking()
            .Select(r => new RoleListRow
            {
                Id = r.Id,
                RolesName = r.RolesName,
                RoleId = r.RoleId,
                IsActive = r.IsActive,
                BypassBusinessUnit = r.BypassBusinessUnit,
                CreatedAtUtc = r.CreatedAtUtc,
            });

        var page = await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<RoleListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<RoleListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    private static RoleListItemDto ToDto(RoleListRow row) => new()
    {
        Id = row.Id,
        RolesName = row.RolesName,
        RoleId = row.RoleId,
        IsActive = row.IsActive,
        BypassBusinessUnit = row.BypassBusinessUnit,
        CreatedAtUtc = row.CreatedAtUtc,
    };
}
