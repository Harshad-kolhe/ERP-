using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Common;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Suppliers;

/// <summary>
/// The shape the database query projects into.
/// <para>
/// It carries the domain <see cref="MasterStatus"/>, not the contract enum. Status is
/// stored as its name (<c>HasConversion&lt;string&gt;</c>), so casting it to
/// <c>MasterStatusDto</c> inside the projection compiles to
/// <c>CAST([s].[Status] AS int)</c> and fails against every non-empty page with
/// "Conversion failed when converting the nvarchar value 'Approved' to data type int".
/// The cast belongs in C#, after materialisation.
/// </para>
/// </summary>
public sealed record SupplierListRow
{
    public required int Id { get; init; }

    public required string? SupplierCode { get; init; }

    public required string? SupplierName { get; init; }

    public required string? SupplierType { get; init; }

    public required string? PrimaryContact { get; init; }

    public required string? SecondaryContact { get; init; }

    public required string? Phone { get; init; }

    public required string? AltPhone { get; init; }

    public required string? Email { get; init; }

    public required string? AltEmail { get; init; }

    public required string? Website { get; init; }

    public required string? BillingAddress { get; init; }

    public required string? BillingCity { get; init; }

    public required string? BillingState { get; init; }

    public required string? BillingCountry { get; init; }

    public required string? BillingZipCode { get; init; }

    public required string? ShippingAddress { get; init; }

    public required string? ShippingCity { get; init; }

    public required string? ShippingState { get; init; }

    public required string? ShippingCountry { get; init; }

    public required string? ShippingZipCode { get; init; }

    public required string? Pan { get; init; }

    public required string? TaxId { get; init; }

    public required string? GstNo { get; init; }

    public required string? BankName { get; init; }

    public required string? AccountNumber { get; init; }

    public required string? Ifsc { get; init; }

    public required string? Swift { get; init; }

    public required string? PaymentTerms { get; init; }

    public required string? Currency { get; init; }

    public required string? TaxCode { get; init; }

    public required string? QualityCompliance { get; init; }

    public required decimal? Igst { get; init; }

    public required decimal? Cgst { get; init; }

    public required decimal? Sgst { get; init; }

    public required string? ActiveStatus { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatus Status { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}

public sealed class SuppliersQueries(ErpDbContext db)
{
    private static readonly QueryMap<SupplierListRow> Map = QueryMap<SupplierListRow>.Create()
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
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<SupplierListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.Suppliers
            .AsNoTracking()
            .Select(s => new SupplierListRow
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
                Status = s.Status,
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

        var page = await rows.ToPagedResultAsync(Map, request, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<SupplierListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<SupplierListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    public async Task<Result<SupplierDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return supplier is null
            ? Result.Failure<SupplierDetailDto>(MasterErrors.NotFound("supplier", id))
            : Result.Success(SupplierMapping.ToDetail(supplier));
    }

    private static SupplierListItemDto ToDto(SupplierListRow row) => new()
    {
        Id = row.Id,
        SupplierCode = row.SupplierCode,
        SupplierName = row.SupplierName,
        SupplierType = row.SupplierType,
        PrimaryContact = row.PrimaryContact,
        SecondaryContact = row.SecondaryContact,
        Phone = row.Phone,
        AltPhone = row.AltPhone,
        Email = row.Email,
        AltEmail = row.AltEmail,
        Website = row.Website,
        BillingAddress = row.BillingAddress,
        BillingCity = row.BillingCity,
        BillingState = row.BillingState,
        BillingCountry = row.BillingCountry,
        BillingZipCode = row.BillingZipCode,
        ShippingAddress = row.ShippingAddress,
        ShippingCity = row.ShippingCity,
        ShippingState = row.ShippingState,
        ShippingCountry = row.ShippingCountry,
        ShippingZipCode = row.ShippingZipCode,
        Pan = row.Pan,
        TaxId = row.TaxId,
        GstNo = row.GstNo,
        BankName = row.BankName,
        AccountNumber = row.AccountNumber,
        Ifsc = row.Ifsc,
        Swift = row.Swift,
        PaymentTerms = row.PaymentTerms,
        Currency = row.Currency,
        TaxCode = row.TaxCode,
        QualityCompliance = row.QualityCompliance,
        Igst = row.Igst,
        Cgst = row.Cgst,
        Sgst = row.Sgst,
        ActiveStatus = row.ActiveStatus,
        IsActive = row.IsActive,
        Status = (MasterStatusDto)row.Status,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };
}
