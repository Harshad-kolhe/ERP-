namespace Erp.Contracts.Masters;

/// <summary>
/// The editable fields of a customer. See <see cref="SaveSupplierRequest"/> for why
/// these are flat and shared between create and update.
/// </summary>
public record SaveCustomerRequest
{
    public required string CustomerName { get; init; }

    public string? Industry { get; init; }

    public string? PrimaryContact { get; init; }

    public string? SecondaryContact { get; init; }

    public string? Phone { get; init; }

    public string? AltPhone { get; init; }

    public string? Email { get; init; }

    public string? AltEmail { get; init; }

    public string? Website { get; init; }

    public string? BillingAddress { get; init; }

    public string? BillingCity { get; init; }

    public string? BillingState { get; init; }

    public string? BillingCountry { get; init; }

    public string? BillingZipCode { get; init; }

    public string? ShippingAddress { get; init; }

    public string? ShippingCity { get; init; }

    public string? ShippingState { get; init; }

    public string? ShippingCountry { get; init; }

    public string? ShippingZipCode { get; init; }

    public string? TaxId { get; init; }

    public string? Gst { get; init; }

    public string? Pan { get; init; }

    /// <summary>Percentage rates, not amounts.</summary>
    public decimal? Igst { get; init; }

    public decimal? Cgst { get; init; }

    public decimal? Sgst { get; init; }

    public string? Currency { get; init; }

    public string? TaxCode { get; init; }

    public bool IsActive { get; init; } = true;

    public MasterStatusDto Status { get; init; } = MasterStatusDto.Draft;
}

public sealed record CreateCustomerRequest : SaveCustomerRequest
{
    /// <summary>Business key. Unique per business unit, and not changed by an ordinary edit.</summary>
    public required string CustomerCode { get; init; }
}

public sealed record UpdateCustomerRequest : SaveCustomerRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}

/// <summary>One customer, as the edit screen loads it. Field names match the request by design.</summary>
public sealed record CustomerDetailDto
{
    public required int Id { get; init; }

    public required string? CustomerCode { get; init; }

    public required string? CustomerName { get; init; }

    public string? Industry { get; init; }

    public string? PrimaryContact { get; init; }

    public string? SecondaryContact { get; init; }

    public string? Phone { get; init; }

    public string? AltPhone { get; init; }

    public string? Email { get; init; }

    public string? AltEmail { get; init; }

    public string? Website { get; init; }

    public string? BillingAddress { get; init; }

    public string? BillingCity { get; init; }

    public string? BillingState { get; init; }

    public string? BillingCountry { get; init; }

    public string? BillingZipCode { get; init; }

    public string? ShippingAddress { get; init; }

    public string? ShippingCity { get; init; }

    public string? ShippingState { get; init; }

    public string? ShippingCountry { get; init; }

    public string? ShippingZipCode { get; init; }

    public string? TaxId { get; init; }

    public string? Gst { get; init; }

    public string? Pan { get; init; }

    public decimal? Igst { get; init; }

    public decimal? Cgst { get; init; }

    public decimal? Sgst { get; init; }

    public string? Currency { get; init; }

    public string? TaxCode { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatusDto Status { get; init; }

    public required int BusinessUnitId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    public required string RowVersion { get; init; }
}
