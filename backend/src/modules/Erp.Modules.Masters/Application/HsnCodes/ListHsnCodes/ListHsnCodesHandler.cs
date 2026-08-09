using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Erp.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.HsnCodes.ListHsnCodes;

internal sealed record ListHsnCodesQuery(PageRequest Page);

/// <summary>
/// One page of HSN codes, each showing the rate in force today.
/// <para>
/// "In force today" is computed in SQL rather than by loading the rate history and
/// picking in memory. The history of a code that has been amended a few times is
/// small, but there is no bound on it, and a list endpoint that materialises a
/// child collection per row is the N+1 the paging rules exist to prevent.
/// </para>
/// </summary>
internal sealed class ListHsnCodesHandler(ErpDbContext db, IClock clock)
    : IQueryHandler<ListHsnCodesQuery, PagedResult<HsnCodeListItemDto>>
{
    private static readonly QueryMap<HsnCodeListItemDto> Map = QueryMap<HsnCodeListItemDto>.Create()
        .Field("code", x => x.Code, searchable: true)
        .Field("description", x => x.Description, searchable: true)
        .Field("currentRatePercent", x => x.CurrentRatePercent)
        .Field("isActive", x => x.IsActive)
        .Field("createdAt", x => x.CreatedAtUtc)
        // By code: HSN numbers group by chapter, so numeric order is subject order.
        .DefaultSort("code")
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<HsnCodeListItemDto>>> HandleAsync(
        ListHsnCodesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var today = clock.Today;

        var rows = db.HsnCodes
            .AsNoTracking()
            .Select(hsn => new HsnCodeListItemDto
            {
                Id = hsn.Id,
                Code = hsn.Code,
                Description = hsn.Description,
                CurrentRatePercent = hsn.Rates
                    .Where(rate => rate.EffectiveFrom <= today)
                    .OrderByDescending(rate => rate.EffectiveFrom)
                    .Select(rate => (decimal?)rate.RatePercent)
                    .FirstOrDefault(),
                IsActive = hsn.IsActive,
                CreatedAtUtc = hsn.CreatedAtUtc,
            });

        return await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);
    }
}
