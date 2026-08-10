using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Parts;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Parts;

public sealed record PartListRow
{
    public required PartId Id { get; init; }

    public required string PartNumber { get; init; }

    public required string OriginalPartNumber { get; init; }

    public required string? ItemNumber { get; init; }

    public required string Description { get; init; }

    public required string? TechnicalSpecification { get; init; }

    public required string? Moc { get; init; }

    public required string? PartCategoryCode { get; init; }

    public required string? PartType { get; init; }

    public required string? FormCategory { get; init; }

    public required string UnitOfMeasureCode { get; init; }

    public required string? PurchaseUomCode { get; init; }

    public required string? SellingUomCode { get; init; }

    public required string? MaterialType { get; init; }

    public required string? SeriesCode { get; init; }

    public required string? PartRevisionNo { get; init; }

    public required string? SourceCode { get; init; }

    public required decimal? WeightKg { get; init; }

    public required int? LeadTimeDays { get; init; }

    public required decimal? MinimumStockLevel { get; init; }

    public required int? ReorderPoint { get; init; }

    public required string? HsnCode { get; init; }

    public required string? DrawingNumber { get; init; }

    public required bool IsActive { get; init; }

    public required PartStatus Status { get; init; }

    public required string? RevisionRemark { get; init; }

    public required string? HoldRemark { get; init; }

    public required string? InactiveRemark { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}

public sealed class PartsQueries(ErpDbContext db)
{
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
        .Field("createdBy", x => x.CreatedBy)
        .Field("createdAt", x => x.CreatedAtUtc)
        .Field("modifiedBy", x => x.ModifiedBy)
        .Field("modifiedAt", x => x.ModifiedAtUtc)
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<PartListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
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

        var page = await rows.ToPagedResultAsync(Map, request, cancellationToken);

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

    public async Task<Result<PartDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var partId = new PartId(id);

        var row = await db.Parts
            .AsNoTracking()
            .Where(p => p.Id == partId)
            .Select(p => new
            {
                p.Id,
                p.PartNumber,
                p.Description,
                p.CategoryId,
                p.UnitOfMeasureCode,
                p.HsnCode,
                p.DrawingNumber,
                p.ItemNumber,
                p.TechnicalSpecification,
                p.Moc,
                p.PartCategoryCode,
                p.PartType,
                p.FormCategory,
                p.PurchaseUomCode,
                p.SellingUomCode,
                p.MaterialType,
                p.SeriesCode,
                p.PartRevisionNo,
                p.SourceCode,
                p.WeightKg,
                p.LeadTimeDays,
                p.MinimumStockLevel,
                p.ReorderPoint,
                p.RevisionRemark,
                p.HoldRemark,
                p.InactiveRemark,
                p.IsActive,
                p.Status,
                p.BusinessUnitId,
                p.CreatedAtUtc,
                p.ModifiedAtUtc,
                p.RowVersion,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result.Failure<PartDetailDto>(PartErrors.NotFound(id));
        }

        return Result.Success(new PartDetailDto
        {
            Id = row.Id.Value,
            PartNumber = row.PartNumber,
            Description = row.Description,
            CategoryId = row.CategoryId,
            UnitOfMeasureCode = row.UnitOfMeasureCode,
            HsnCode = row.HsnCode,
            DrawingNumber = row.DrawingNumber,
            Attributes = new PartAttributesDto
            {
                ItemNumber = row.ItemNumber,
                TechnicalSpecification = row.TechnicalSpecification,
                Moc = row.Moc,
                PartCategoryCode = row.PartCategoryCode,
                PartType = row.PartType,
                FormCategory = row.FormCategory,
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
                RevisionRemark = row.RevisionRemark,
                HoldRemark = row.HoldRemark,
                InactiveRemark = row.InactiveRemark,
            },
            IsActive = row.IsActive,
            Status = (PartStatusDto)row.Status,
            BusinessUnitId = row.BusinessUnitId,
            CreatedAtUtc = row.CreatedAtUtc,
            ModifiedAtUtc = row.ModifiedAtUtc,
            RowVersion = Convert.ToBase64String(row.RowVersion),
        });
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
