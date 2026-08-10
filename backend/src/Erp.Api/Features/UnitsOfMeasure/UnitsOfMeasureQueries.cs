using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.UnitsOfMeasure;

public sealed class UnitsOfMeasureQueries(ErpDbContext db)
{
    private static readonly QueryMap<UnitOfMeasureListItemDto> Map = QueryMap<UnitOfMeasureListItemDto>.Create()
        .Field("code", x => x.Code, searchable: true)
        .Field("name", x => x.Name, searchable: true)
        .Field("decimals", x => x.Decimals)
        .Field("baseUnitCode", x => x.BaseUnitCode)
        .Field("conversionToBase", x => x.ConversionToBase)
        .Field("sortOrder", x => x.SortOrder)
        .Field("isActive", x => x.IsActive)
        .Field("createdAt", x => x.CreatedAtUtc)
        .DefaultSort("sortOrder")
        .TieBreaker(x => x.Id)
        .Build();

    public Task<Result<PagedResult<UnitOfMeasureListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.UnitsOfMeasure
            .AsNoTracking()
            .Select(unit => new UnitOfMeasureListItemDto
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                Decimals = unit.Decimals,
                BaseUnitCode = unit.BaseUnitCode,
                ConversionToBase = unit.ConversionToBase,
                SortOrder = unit.SortOrder,
                IsActive = unit.IsActive,
                CreatedAtUtc = unit.CreatedAtUtc,
            });

        return rows.ToPagedResultAsync(Map, request, cancellationToken);
    }

    public async Task<Result<UnitOfMeasureDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await db.UnitsOfMeasure
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return unit is null
            ? Result.Failure<UnitOfMeasureDetailDto>(MasterErrors.NotFound("unit of measure", id))
            : Result.Success(UnitOfMeasureMapping.ToDetail(unit));
    }
}
