using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.LookupValues.ListLookupValues;

internal sealed record ListLookupValuesQuery(PageRequest Page);

/// <summary>
/// One page of the reference-data grid.
/// <para>
/// Every list in the system is in this one table, so the grid is filtered by
/// <c>type</c> rather than split into twenty screens. That is also why
/// <c>type</c> is searchable: an administrator looking for "the source code list"
/// types <c>source</c> and gets there without knowing the exact key.
/// </para>
/// </summary>
internal sealed class ListLookupValuesHandler(ErpDbContext db)
    : IQueryHandler<ListLookupValuesQuery, PagedResult<LookupValueListItemDto>>
{
    private static readonly QueryMap<LookupValueListItemDto> Map = QueryMap<LookupValueListItemDto>.Create()
        .Field("type", x => x.Type, searchable: true)
        .Field("code", x => x.Code, searchable: true)
        .Field("name", x => x.Name, searchable: true)
        .Field("sortOrder", x => x.SortOrder)
        .Field("isActive", x => x.IsActive)
        .Field("createdAt", x => x.CreatedAtUtc)
        // By list, then by the order the list is meant to be shown in — not newest
        // first as the record masters use. Reference data is read as lists, and a
        // grid that interleaves twenty of them by creation date is unusable.
        .DefaultSort("type")
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<LookupValueListItemDto>>> HandleAsync(
        ListLookupValuesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

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

        return await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);
    }
}
