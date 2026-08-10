using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Paging;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Api.Persistence;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Suppliers.ListSuppliers;

/// <summary>
/// Returns one page of suppliers.
/// <para>
/// Like <c>ListPartsHandler</c>, this never loads a <c>Supplier</c> aggregate. It
/// projects to the columns the grid renders and lets the database filter, sort,
/// count and page.
/// </para>
/// </summary>
public sealed class ListSuppliersHandler(ErpDbContext db)
    : IQueryHandler<ListSuppliersQuery, PagedResult<SupplierListItemDto>>
{
    /// <summary>
    /// The allow-list. A field absent here cannot be sorted or filtered on, no
    /// matter what the client sends.
    /// <para>
    /// Free-text search stays on the five identifiers people actually type. Every
    /// searchable field is another <c>LIKE '%â€¦%'</c> per keystroke, and nobody
    /// searches suppliers by shipping city.
    /// </para>
    /// </summary>
    private static readonly QueryMap<SupplierListItemDto> Map = QueryMap<SupplierListItemDto>.Create()
        .Field("supplierCode", x => x.SupplierCode, searchable: true)
        .Field("supplierName", x => x.SupplierName, searchable: true)
        .Field("supplierType", x => x.SupplierType)
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
        .Field("pan", x => x.Pan)
        .Field("taxId", x => x.TaxId)
        .Field("gstNo", x => x.GstNo, searchable: true)
        .Field("bankName", x => x.BankName)
        .Field("accountNumber", x => x.AccountNumber)
        .Field("ifsc", x => x.Ifsc)
        .Field("swift", x => x.Swift)
        .Field("paymentTerms", x => x.PaymentTerms)
        .Field("currency", x => x.Currency)
        .Field("taxCode", x => x.TaxCode)
        .Field("qualityCompliance", x => x.QualityCompliance)
        .Field("igst", x => x.Igst)
        .Field("cgst", x => x.Cgst)
        .Field("sgst", x => x.Sgst)
        .Field("activeStatus", x => x.ActiveStatus)
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

    public async Task<Result<PagedResult<SupplierListItemDto>>> HandleAsync(
        ListSuppliersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Tenancy and soft-delete filters are already on this query â€” applied by
        // convention in ErpDbContextBase, not requested here.
        var rows = db.Suppliers
            .AsNoTracking()
            .Select(s => new SupplierListItemDto
            {
                Id = s.Id,
                SupplierCode = s.SupplierCode,
                SupplierName = s.SupplierName,
                SupplierType = s.SupplierType,
                PrimaryContact = s.PrimaryContact,
                SecondaryContact = s.SecondaryContact,
                Phone = s.Phone,
                AltPhone = s.AltPhone,
                Email = s.Email,
                AltEmail = s.AltEmail,
                Website = s.Website,
                BillingAddress = s.BillingAddress,
                BillingCity = s.BillingCity,
                BillingState = s.BillingState,
                BillingCountry = s.BillingCountry,
                BillingZipCode = s.BillingZipCode,
                ShippingAddress = s.ShippingAddress,
                ShippingCity = s.ShippingCity,
                ShippingState = s.ShippingState,
                ShippingCountry = s.ShippingCountry,
                ShippingZipCode = s.ShippingZipCode,
                Pan = s.Pan,
                TaxId = s.TaxId,
                GstNo = s.GstNo,
                BankName = s.BankName,
                AccountNumber = s.AccountNumber,
                Ifsc = s.Ifsc,
                Swift = s.Swift,
                PaymentTerms = s.PaymentTerms,
                Currency = s.Currency,
                TaxCode = s.TaxCode,
                QualityCompliance = s.QualityCompliance,
                Igst = s.Igst,
                Cgst = s.Cgst,
                Sgst = s.Sgst,
                ActiveStatus = s.ActiveStatus,
                IsActive = s.IsActive,
                Status = (MasterStatusDto)s.Status,
                CreatedBy = db.Users
                    .Where(u => u.Id == s.CreatedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                CreatedAtUtc = s.CreatedAtUtc,
                ModifiedBy = db.Users
                    .Where(u => u.Id == s.ModifiedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ModifiedAtUtc = s.ModifiedAtUtc,
            });

        return await rows.ToPagedResultAsync(Map, query.Page, cancellationToken);
    }
}
