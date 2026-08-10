using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Common.Time;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.HsnCodes;

public sealed class HsnCodesQueries(ErpDbContext db, IClock clock)
{
    private static readonly QueryMap<HsnCodeListItemDto> Map = QueryMap<HsnCodeListItemDto>.Create()
        .Field("code", x => x.Code, searchable: true)
        .Field("description", x => x.Description, searchable: true)
        .Field("currentRatePercent", x => x.CurrentRatePercent)
        .Field("isActive", x => x.IsActive)
        .Field("createdAt", x => x.CreatedAtUtc)
        .DefaultSort("code")
        .TieBreaker(x => x.Id)
        .Build();

    public Task<Result<PagedResult<HsnCodeListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
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

        return rows.ToPagedResultAsync(Map, request, cancellationToken);
    }

    public async Task<Result<HsnCodeDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var hsn = await db.HsnCodes
            .AsNoTracking()
            .Include(h => h.Rates)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        return hsn is null
            ? Result.Failure<HsnCodeDetailDto>(MasterErrors.NotFound("HSN code", id))
            : Result.Success(HsnCodeMapping.ToDetail(hsn));
    }
}
