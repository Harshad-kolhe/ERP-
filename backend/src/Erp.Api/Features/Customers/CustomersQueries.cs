using Erp.Api.Common.Paging;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Common;
using Erp.Api.Features.Masters;
using Erp.Api.Persistence;
using Erp.Api.Persistence.Paging;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Customers;

/// <summary>
/// The shape the database query projects into. Carries the domain
/// <see cref="MasterStatus"/> for the same reason <c>SupplierListRow</c> does: the
/// column holds the status name, so the cast to the contract enum has to happen in
/// C# rather than as a SQL <c>CAST(... AS int)</c>.
/// </summary>
public sealed record CustomerListRow
{
    public required int Id { get; init; }

    public required string? CustomerCode { get; init; }

    public required string? CustomerName { get; init; }

    public required string? Industry { get; init; }

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

    public required string? TaxId { get; init; }

    public required string? Gst { get; init; }

    public required string? Pan { get; init; }

    public required decimal? Igst { get; init; }

    public required decimal? Cgst { get; init; }

    public required decimal? Sgst { get; init; }

    public required string? Currency { get; init; }

    public required string? TaxCode { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatus Status { get; init; }

    public required string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string? ModifiedBy { get; init; }

    public required DateTimeOffset? ModifiedAtUtc { get; init; }
}

public sealed class CustomersQueries(ErpDbContext db)
{
    private static readonly QueryMap<CustomerListRow> Map = QueryMap<CustomerListRow>.Create()
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
        .DefaultSort("createdAt", descending: true)
        .TieBreaker(x => x.Id)
        .Build();

    public async Task<Result<PagedResult<CustomerListItemDto>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var rows = db.Customers
            .AsNoTracking()
            .Select(c => new CustomerListRow
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
                Status = c.Status,
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

        var page = await rows.ToPagedResultAsync(Map, request, cancellationToken);

        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<CustomerListItemDto>>(page.Error);
        }

        var items = page.Value.Items.Select(ToDto).ToList();

        return Result.Success(new PagedResult<CustomerListItemDto>(
            items,
            page.Value.Page,
            page.Value.PageSize,
            page.Value.TotalCount));
    }

    public async Task<Result<CustomerDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer is null
            ? Result.Failure<CustomerDetailDto>(MasterErrors.NotFound("customer", id))
            : Result.Success(CustomerMapping.ToDetail(customer));
    }

    private static CustomerListItemDto ToDto(CustomerListRow row) => new()
    {
        Id = row.Id,
        CustomerCode = row.CustomerCode,
        CustomerName = row.CustomerName,
        Industry = row.Industry,
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
        TaxId = row.TaxId,
        Gst = row.Gst,
        Pan = row.Pan,
        Igst = row.Igst,
        Cgst = row.Cgst,
        Sgst = row.Sgst,
        Currency = row.Currency,
        TaxCode = row.TaxCode,
        IsActive = row.IsActive,
        Status = (MasterStatusDto)row.Status,
        CreatedBy = row.CreatedBy,
        CreatedAtUtc = row.CreatedAtUtc,
        ModifiedBy = row.ModifiedBy,
        ModifiedAtUtc = row.ModifiedAtUtc,
    };
}
