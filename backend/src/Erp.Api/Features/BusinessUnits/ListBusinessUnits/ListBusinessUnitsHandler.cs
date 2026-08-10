using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Paging;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Api.Persistence;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.BusinessUnits.ListBusinessUnits;

/// <summary>
/// Returns one page of business units.
/// <para>
/// Every caller sees every unit, because this table is not tenant-scoped â€” it is
/// what the tenancy scope is defined in terms of. The soft-delete filter still
/// applies. Access rests entirely on the endpoint's permission.
/// </para>
/// </summary>
public sealed class ListBusinessUnitsHandler(ErpDbContext db)
    : IQueryHandler<ListBusinessUnitsQuery, PagedResult<BusinessUnitListItemDto>>
{
    private static readonly QueryMap<BusinessUnitListItemDto> Map = QueryMap<BusinessUnitListItemDto>.Create()
        .Field("businessUnitId", x => x.BusinessUnitId)
        .Field("businessName", x => x.BusinessName, searchable: true)
        .Field("address", x => x.Address)
        .Field("contactNumber", x => x.ContactNumber)
        .Field("email", x => x.Email, searchable: true)
        .Field("website", x => x.Website)
        .Field("cin", x => x.Cin, searchable: true)
        .Field("gstn", x => x.Gstn, searchable: true)
        .Field("stateName", x => x.StateName)
        .Field("isActive", x => x.IsActive)
        .Field("createdAt", x => x.CreatedAtUtc)
        // Newest first: a master is worked from the end, and the row somebody
        // just added is the one they came back to check. Any column header still
        // reorders it, and the tie-breaker below keeps paging stable either way.
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<BusinessUnitListItemDto>>> HandleAsync(
        ListBusinessUnitsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = db.BusinessUnits
            .AsNoTracking()
            .Select(b => new BusinessUnitListItemDto
            {
                Id = b.Id,
                BusinessUnitId = b.BusinessUnitId,
                BusinessName = b.BusinessName,
                Address = b.Address,
                ContactNumber = b.ContactNumber,
                Email = b.Email,
                Website = b.Website,
                Cin = b.Cin,
                Gstn = b.Gstn,
                StateName = b.StateName,
                IsActive = b.IsActive,
                CreatedAtUtc = b.CreatedAtUtc,
            });

        return await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);
    }
}
