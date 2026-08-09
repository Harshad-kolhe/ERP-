using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.ListParts;

/// <summary>
/// Returns one page of parts.
/// <para>
/// Note what this handler does <em>not</em> do: it never loads a <c>Part</c>
/// aggregate. It projects straight to the columns the grid renders, and the
/// database does the filtering, sorting, counting and paging. In the system this
/// replaces, roughly 149 of 180 grids fetched every matching row into the browser
/// and paged there.
/// </para>
/// <para>
/// The grid is wide because the legacy Part Master is, and a wide projection is
/// still one query: the two author names come from a left join onto the audit-user
/// view rather than from a lookup per row.
/// </para>
/// </summary>
internal sealed class ListPartsHandler(ErpDbContext db)
    : IQueryHandler<ListPartsQuery, PagedResult<PartListItemDto>>
{
    /// <summary>
    /// The allow-list. A field absent here cannot be sorted or filtered on, no
    /// matter what the client sends.
    /// <para>
    /// Free-text search stays on the four fields people actually search by. Adding
    /// the rest would turn every keystroke into a <c>LIKE '%…%'</c> across twenty
    /// columns, none of which an index can help with, to answer a question nobody
    /// asks.
    /// </para>
    /// </summary>
    private static readonly QueryMap<PartListRow> Map = QueryMap<PartListRow>.Create()
        .Field("partNumber", x => x.PartNumber, searchable: true)
        .Field("originalPartNumber", x => x.OriginalPartNumber, searchable: true)
        .Field("itemNumber", x => x.ItemNumber, searchable: true)
        .Field("description", x => x.Description, searchable: true)
        .Field("technicalSpecification", x => x.TechnicalSpecification)
        .Field("moc", x => x.Moc)
        .Field("partCategoryCode", x => x.PartCategoryCode)
        .Field("partType", x => x.PartType)
        .Field("formCategory", x => x.FormCategory)
        .Field("unitOfMeasureCode", x => x.UnitOfMeasureCode)
        .Field("purchaseUomCode", x => x.PurchaseUomCode)
        .Field("sellingUomCode", x => x.SellingUomCode)
        .Field("materialType", x => x.MaterialType)
        .Field("seriesCode", x => x.SeriesCode)
        .Field("partRevisionNo", x => x.PartRevisionNo)
        .Field("sourceCode", x => x.SourceCode)
        .Field("weightKg", x => x.WeightKg)
        .Field("leadTimeDays", x => x.LeadTimeDays)
        .Field("minimumStockLevel", x => x.MinimumStockLevel)
        .Field("reorderPoint", x => x.ReorderPoint)
        .Field("hsnCode", x => x.HsnCode, searchable: true)
        .Field("drawingNumber", x => x.DrawingNumber)
        .Field("isActive", x => x.IsActive)
        .Field("status", x => x.Status)
        .Field("revisionRemark", x => x.RevisionRemark)
        .Field("holdRemark", x => x.HoldRemark)
        .Field("inactiveRemark", x => x.InactiveRemark)
        // Sorting on these two orders by the joined display name, which no index
        // covers. Allowed because a master table is thousands of rows, not millions,
        // and a column that sorts differently from its neighbours is a worse answer
        // than a sort that scans.
        .Field("createdBy", x => x.CreatedBy)
        .Field("createdAt", x => x.CreatedAtUtc)
        .Field("modifiedBy", x => x.ModifiedBy)
        .Field("modifiedAt", x => x.ModifiedAtUtc)
        // Newest first: a master is worked from the end, and the row somebody
        // just added is the one they came back to check. Any column header still
        // reorders it, and the tie-breaker below keeps paging stable either way.
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<PartListItemDto>>> HandleAsync(
        ListPartsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The tenancy and soft-delete filters are already on this query: they are
        // applied by convention in ErpDbContextBase, not requested here.
        //
        // The author names are correlated subqueries rather than a join in the query
        // syntax: they read as what they are — "the name for this id, if there is
        // one" — and SQL Server plans them as the same left join either way.
        var rows = db.Parts
            .AsNoTracking()
            .Select(p => new PartListRow
            {
                Id = p.Id,
                PartNumber = p.PartNumber,
                OriginalPartNumber = p.OriginalPartNumber,
                ItemNumber = p.ItemNumber,
                Description = p.Description,
                TechnicalSpecification = p.TechnicalSpecification,
                Moc = p.Moc,
                PartCategoryCode = p.PartCategoryCode,
                PartType = p.PartType,
                FormCategory = p.FormCategory,
                UnitOfMeasureCode = p.UnitOfMeasureCode,
                PurchaseUomCode = p.PurchaseUomCode,
                SellingUomCode = p.SellingUomCode,
                MaterialType = p.MaterialType,
                SeriesCode = p.SeriesCode,
                PartRevisionNo = p.PartRevisionNo,
                SourceCode = p.SourceCode,
                WeightKg = p.WeightKg,
                LeadTimeDays = p.LeadTimeDays,
                MinimumStockLevel = p.MinimumStockLevel,
                ReorderPoint = p.ReorderPoint,
                HsnCode = p.HsnCode,
                DrawingNumber = p.DrawingNumber,
                IsActive = p.IsActive,
                Status = p.Status,
                RevisionRemark = p.RevisionRemark,
                HoldRemark = p.HoldRemark,
                InactiveRemark = p.InactiveRemark,
                CreatedBy = db.Users
                    .Where(u => u.Id == p.CreatedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = p.CreatedAtUtc,
                ModifiedBy = db.Users
                    .Where(u => u.Id == p.ModifiedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = p.ModifiedAtUtc,
            });

        var page = await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<PartListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<PartListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    private static PartListItemDto ToDto(PartListRow row) => new()
    {
        Id = row.Id.Value,
        PartNumber = row.PartNumber,
        OriginalPartNumber = row.OriginalPartNumber,
        ItemNumber = row.ItemNumber,
        Description = row.Description,
        TechnicalSpecification = row.TechnicalSpecification,
        Moc = row.Moc,
        PartCategoryCode = row.PartCategoryCode,
        PartType = row.PartType,
        FormCategory = row.FormCategory,
        UnitOfMeasureCode = row.UnitOfMeasureCode,
        PurchaseUomCode = row.PurchaseUomCode,
        SellingUomCode = row.SellingUomCode,
        MaterialType = row.MaterialType,
        SeriesCode = row.SeriesCode,
        PartRevisionNo = row.PartRevisionNo,
        SourceCode = row.SourceCode,
        WeightKg = row.WeightKg,
        LeadTimeDays = row.LeadTimeDays,
        MinimumStockLevel = row.MinimumStockLevel,
        ReorderPoint = row.ReorderPoint,
        HsnCode = row.HsnCode,
        DrawingNumber = row.DrawingNumber,
        IsActive = row.IsActive,
        Status = (PartStatusDto)row.Status,
        RevisionRemark = row.RevisionRemark,
        HoldRemark = row.HoldRemark,
        InactiveRemark = row.InactiveRemark,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };
}
