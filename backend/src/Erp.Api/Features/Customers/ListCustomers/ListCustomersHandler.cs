using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Paging;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Api.Persistence;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Customers.ListCustomers;

/// <summary>
/// Returns one page of customers. Projected, never materialised as aggregates â€”
/// see <c>ListPartsHandler</c> for the reasoning.
/// </summary>
public sealed class ListCustomersHandler(ErpDbContext db)
    : IQueryHandler<ListCustomersQuery, PagedResult<CustomerListItemDto>>
{
    /// <summary>The allow-list. Anything absent here cannot be sorted or filtered on.</summary>
    private static readonly QueryMap<CustomerListItemDto> Map = QueryMap<CustomerListItemDto>.Create()
        .Field("customerCode", x => x.CustomerCode, searchable: true)
        .Field("customerName", x => x.CustomerName, searchable: true)
        .Field("industry", x => x.Industry)
        .Field("primaryContact", x => x.PrimaryContact, searchable: true)
        .Field("secondaryContact", x => x.SecondaryContact)
        .Field("phone", x => x.Phone)
        .Field("altPhone", x => x.AltPhone)
        .Field("email", x => x.Email, searchable: true)
        .Field("altEmail", x => x.AltEmail)
        .Field("website", x => x.Website)
        .Field("billingAddress", x => x.BillingAddress)
        .Field("billingCity", x => x.BillingCity)
        .Field("billingState", x => x.BillingState)
        .Field("billingCountry", x => x.BillingCountry)
        .Field("billingZipCode", x => x.BillingZipCode)
        .Field("shippingAddress", x => x.ShippingAddress)
        .Field("shippingCity", x => x.ShippingCity)
        .Field("shippingState", x => x.ShippingState)
        .Field("shippingCountry", x => x.ShippingCountry)
        .Field("shippingZipCode", x => x.ShippingZipCode)
        .Field("taxId", x => x.TaxId)
        .Field("gst", x => x.Gst, searchable: true)
        .Field("pan", x => x.Pan)
        .Field("igst", x => x.Igst)
        .Field("cgst", x => x.Cgst)
        .Field("sgst", x => x.Sgst)
        .Field("currency", x => x.Currency)
        .Field("taxCode", x => x.TaxCode)
        .Field("isActive", x => x.IsActive)
        .Field("status", x => x.Status)
        .Field("createdBy", x => x.CreatedBy)
        .Field("createdAt", x => x.CreatedAtUtc)
        .Field("modifiedBy", x => x.ModifiedBy)
        .Field("modifiedAt", x => x.ModifiedAtUtc)
        // Newest first: a master is worked from the end, and the row somebody
        // just added is the one they came back to check. Any column header still
        // reorders it, and the tie-breaker below keeps paging stable either way.
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<CustomerListItemDto>>> HandleAsync(
        ListCustomersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = db.Customers
            .AsNoTracking()
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Industry = c.Industry,
                PrimaryContact = c.PrimaryContact,
                SecondaryContact = c.SecondaryContact,
                Phone = c.Phone,
                AltPhone = c.AltPhone,
                Email = c.Email,
                AltEmail = c.AltEmail,
                Website = c.Website,
                BillingAddress = c.BillingAddress,
                BillingCity = c.BillingCity,
                BillingState = c.BillingState,
                BillingCountry = c.BillingCountry,
                BillingZipCode = c.BillingZipCode,
                ShippingAddress = c.ShippingAddress,
                ShippingCity = c.ShippingCity,
                ShippingState = c.ShippingState,
                ShippingCountry = c.ShippingCountry,
                ShippingZipCode = c.ShippingZipCode,
                TaxId = c.TaxId,
                Gst = c.Gst,
                Pan = c.Pan,
                Igst = c.Igst,
                Cgst = c.Cgst,
                Sgst = c.Sgst,
                Currency = c.Currency,
                TaxCode = c.TaxCode,
                IsActive = c.IsActive,
                Status = (MasterStatusDto)c.Status,
                CreatedBy = db.Users
                    .Where(u => u.Id == c.CreatedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = c.CreatedAtUtc,
                ModifiedBy = db.Users
                    .Where(u => u.Id == c.ModifiedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = c.ModifiedAtUtc,
            });

        return await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);
    }
}
