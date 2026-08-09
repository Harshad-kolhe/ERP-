using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Assemblies.ListAssemblyNodes;

/// <param name="Level">
/// Which of the three grids is asking. Supplied by the endpoint from its route, not
/// by the client — see <c>CreateAssemblyNodeRequest</c> for why the level never
/// travels in a payload.
/// </param>
internal sealed record ListAssemblyNodesQuery(AssemblyLevel Level, PageRequest Page);

/// <summary>
/// Returns one page of sections, assemblies or sub-assemblies.
/// <para>
/// One handler for all three grids. The legacy screen ran
/// <c>EXEC USP_GET_SASAP 'SL'</c> and two sibling procedures that each returned the
/// whole table to the browser, which then paged it; here the level is a
/// <c>WHERE</c> clause and the database does the filtering, sorting, counting and
/// paging.
/// </para>
/// <para>
/// The parent code, parent name and child count are correlated subqueries rather
/// than per-row lookups: they read as what they are, and SQL Server plans them as
/// joins. The legacy grid fetched the parent list separately and matched it up in
/// JavaScript.
/// </para>
/// </summary>
internal sealed class ListAssemblyNodesHandler(MastersDbContext db)
    : IQueryHandler<ListAssemblyNodesQuery, PagedResult<AssemblyNodeListItemDto>>
{
    /// <summary>
    /// The allow-list. A field absent here cannot be sorted or filtered on, no
    /// matter what the client sends.
    /// <para>
    /// <c>childCount</c> is deliberately absent: it is a subquery, so sorting on it
    /// makes the database count every node's children before it can order the page.
    /// The number is worth showing and not worth sorting by.
    /// </para>
    /// </summary>
    private static readonly QueryMap<AssemblyNodeListRow> Map = QueryMap<AssemblyNodeListRow>.Create()
        .Field("code", x => x.Code, searchable: true)
        .Field("name", x => x.Name, searchable: true)
        .Field("manualCode", x => x.ManualCode, searchable: true)
        .Field("parentCode", x => x.ParentCode, searchable: true)
        .Field("parentName", x => x.ParentName)
        .Field("machineType", x => x.MachineType)
        .Field("drivenBy", x => x.DrivenBy)
        .Field("drawingPath", x => x.DrawingPath)
        .Field("technicalSpecification", x => x.TechnicalSpecification)
        .Field("remark", x => x.Remark)
        .Field("quantity", x => x.Quantity)
        .Field("weightKg", x => x.WeightKg)
        .Field("displaySequence", x => x.DisplaySequence)
        .Field("isActive", x => x.IsActive)
        .Field("createdBy", x => x.CreatedBy)
        .Field("createdAt", x => x.CreatedAtUtc)
        .Field("modifiedBy", x => x.ModifiedBy)
        .Field("modifiedAt", x => x.ModifiedAtUtc)
        // Sequence first, because that is the order the engineering drawings are in
        // and the order people read these lists in. Nulls sort first on SQL Server,
        // which puts unsequenced nodes at the top where they are noticed and given
        // one — the legacy grid ordered by id, so a node inserted later appeared
        // last regardless of where it belonged.
        .DefaultSort("displaySequence")
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<AssemblyNodeListItemDto>>> HandleAsync(
        ListAssemblyNodesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The tenancy and soft-delete filters are already on this query: they are
        // applied by convention in ErpDbContextBase, not requested here.
        var rows = db.AssemblyNodes
            .AsNoTracking()
            .Where(node => node.Level == query.Level)
            .Select(node => new AssemblyNodeListRow
            {
                Id = node.Id,
                Code = node.Code,
                Name = node.Name,
                ManualCode = node.ManualCode,
                Level = node.Level,
                ParentId = node.ParentId,
                ParentCode = db.AssemblyNodes
                    .Where(parent => parent.Id == node.ParentId)
                    .Select(parent => parent.Code)
                    .FirstOrDefault(),
                ParentName = db.AssemblyNodes
                    .Where(parent => parent.Id == node.ParentId)
                    .Select(parent => parent.Name)
                    .FirstOrDefault(),
                ChildCount = db.AssemblyNodes.Count(child => child.ParentId == node.Id),
                MachineType = node.MachineType,
                DrivenBy = node.DrivenBy,
                DrawingPath = node.DrawingPath,
                TechnicalSpecification = node.TechnicalSpecification,
                Remark = node.Remark,
                Quantity = node.Quantity,
                WeightKg = node.WeightKg,
                DisplaySequence = node.DisplaySequence,
                IsActive = node.IsActive,
                CreatedBy = db.AuditUsers
                    .Where(user => user.Id == node.CreatedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = node.CreatedAtUtc,
                ModifiedBy = db.AuditUsers
                    .Where(user => user.Id == node.ModifiedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = node.ModifiedAtUtc,
            });

        var page = await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<AssemblyNodeListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<AssemblyNodeListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    private static AssemblyNodeListItemDto ToDto(AssemblyNodeListRow row) => new()
    {
        Id = row.Id.Value,
        Code = row.Code,
        Name = row.Name,
        ManualCode = row.ManualCode,
        Level = AssemblyNodeMapping.ToDto(row.Level),
        ParentId = row.ParentId?.Value,
        ParentCode = row.ParentCode,
        ParentName = row.ParentName,
        ChildCount = row.ChildCount,
        MachineType = row.MachineType,
        DrivenBy = row.DrivenBy,
        DrawingPath = row.DrawingPath,
        TechnicalSpecification = row.TechnicalSpecification,
        Remark = row.Remark,
        Quantity = row.Quantity,
        WeightKg = row.WeightKg,
        DisplaySequence = row.DisplaySequence,
        IsActive = row.IsActive,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };
}
