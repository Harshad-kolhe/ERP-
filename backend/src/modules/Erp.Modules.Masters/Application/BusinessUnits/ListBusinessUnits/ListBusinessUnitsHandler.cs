using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Application.Querying;
using Erp.BuildingBlocks.Persistence.Querying;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.BusinessUnits.ListBusinessUnits;

/// <summary>
/// Returns one page of business units.
/// <para>
/// Every caller sees every unit, because this table is not tenant-scoped — it is
/// what the tenancy scope is defined in terms of. The soft-delete filter still
/// applies. Access rests entirely on the endpoint's permission.
/// </para>
/// </summary>
internal sealed class ListBusinessUnitsHandler(ErpDbContext db)
    : IQueryHandler<ListBusinessUnitsQuery, PagedResult<BusinessUnitListItemDto>>
{
    private static readonly QueryMap<BusinessUnitListRow> Map = QueryMap<BusinessUnitListRow>.Create()
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
            .Select(b => new BusinessUnitListRow
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

        var page = await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<BusinessUnitListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<BusinessUnitListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    private static BusinessUnitListItemDto ToDto(BusinessUnitListRow row) => new()
    {
        Id = row.Id,
        BusinessUnitId = row.BusinessUnitId,
        BusinessName = row.BusinessName,
        Address = row.Address,
        ContactNumber = row.ContactNumber,
        Email = row.Email,
        Website = row.Website,
        Cin = row.Cin,
        Gstn = row.Gstn,
        StateName = row.StateName,
        IsActive = row.IsActive,
        CreatedAtUtc = row.CreatedAtUtc,
    };
}
