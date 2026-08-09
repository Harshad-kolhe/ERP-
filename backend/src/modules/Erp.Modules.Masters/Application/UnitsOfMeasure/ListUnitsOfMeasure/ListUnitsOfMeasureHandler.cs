using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.UnitsOfMeasure.ListUnitsOfMeasure;

internal sealed record ListUnitsOfMeasureQuery(PageRequest Page);

internal sealed class ListUnitsOfMeasureHandler(ErpDbContext db)
    : IQueryHandler<ListUnitsOfMeasureQuery, PagedResult<UnitOfMeasureListItemDto>>
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
        // Display order, which is the order the dropdown will show — NOS first, not
        // AMP. Sorting a unit list alphabetically hides the one everybody wants.
        .DefaultSort("sortOrder")
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<UnitOfMeasureListItemDto>>> HandleAsync(
        ListUnitsOfMeasureQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

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

        return await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);
    }
}
