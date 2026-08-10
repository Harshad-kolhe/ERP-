using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.LookupValues;

public sealed class LookupValuesQueries(ErpDbContext db)
{
    private static readonly QueryMap<LookupValueListItemDto> Map = QueryMap<LookupValueListItemDto>.Create()
        .Field("type", x => x.Type, searchable: true)
        .Field("code", x => x.Code, searchable: true)
        .Field("name", x => x.Name, searchable: true)
        .Field("sortOrder", x => x.SortOrder)
        .Field("isActive", x => x.IsActive)
        .Field("createdAt", x => x.CreatedAtUtc)
        .DefaultSort("type")
        .TieBreaker(x => x.Id)
        .Build();

    public Task<Result<PagedResult<LookupValueListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.LookupValues
            .AsNoTracking()
            .Select(value => new LookupValueListItemDto
            {
                Id = value.Id,
                Type = value.Type,
                Code = value.Code,
                Name = value.Name,
                SortOrder = value.SortOrder,
                IsActive = value.IsActive,
                CreatedAtUtc = value.CreatedAtUtc,
            });

        return rows.ToPagedResultAsync(Map, request, cancellationToken);
    }

    public async Task<Result<LookupValueDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var value = await db.LookupValues
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return value is null
            ? Result.Failure<LookupValueDetailDto>(MasterErrors.NotFound("lookup value", id))
            : Result.Success(LookupValueMapping.ToDetail(value));
    }
}
