using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.BusinessUnits;

public sealed class BusinessUnitsQueries(ErpDbContext db)
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
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public Task<Result<PagedResult<BusinessUnitListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
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

        return rows.ToPagedResultAsync(Map, request, cancellationToken);
    }

    public async Task<Result<BusinessUnitDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await db.BusinessUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return unit is null
            ? Result.Failure<BusinessUnitDetailDto>(MasterErrors.NotFound("business unit", id))
            : Result.Success(BusinessUnitMapping.ToDetail(unit));
    }
}
