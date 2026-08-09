using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Domain.Parts;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.GetPartById;

internal sealed record GetPartByIdQuery(Guid Id);

internal sealed class GetPartByIdHandler(MastersDbContext db) : IQueryHandler<GetPartByIdQuery, PartDetailDto>
{
    public async Task<Result<PartDetailDto>> HandleAsync(
        GetPartByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var partId = new PartId(query.Id);

        // Projected to an anonymous row first. Neither `p.Id.Value` nor
        // `Convert.ToBase64String(...)` is translatable to SQL — the first because
        // `.Value` is a member of the strongly-typed id struct rather than of the
        // mapping, the second because SQL Server has no equivalent. Both are done
        // in memory, over a single row, once the columns are back.
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

        // A part in another business unit is filtered out by the global query
        // filter, so it is indistinguishable from one that does not exist. That is
        // deliberate: a 404 rather than a 403 avoids confirming that the id is real.
        if (row is null)
        {
            return Result.Failure<PartDetailDto>(PartErrors.NotFound(query.Id));
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
}
