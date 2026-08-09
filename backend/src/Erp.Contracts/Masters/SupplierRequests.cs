namespace Erp.Contracts.Masters;

/// <summary>
/// The editable fields of a supplier — everything except the code, which is the
/// business key, and the audit stamps, which the server owns.
/// <para>
/// Flat rather than grouped into sub-objects. The form is one form, and a nested
/// payload means the server reports a failure as <c>Bank.Ifsc</c> while the input
/// is called <c>ifsc</c>, so the message has to be matched back by hand before it
/// can appear under the field that caused it.
/// </para>
/// <para>
/// Shared by create and update so the two cannot drift: <see cref="CreateSupplierRequest"/>
/// adds the code, <see cref="UpdateSupplierRequest"/> adds the row version.
/// </para>
/// </summary>
public record SaveSupplierRequest
{
    public required string SupplierName { get; init; }

    public string? SupplierType { get; init; }

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

    public string? Pan { get; init; }

    public string? TaxId { get; init; }

    public string? GstNo { get; init; }

    public string? BankName { get; init; }

    public string? AccountNumber { get; init; }

    public string? Ifsc { get; init; }

    public string? Swift { get; init; }

    public string? PaymentTerms { get; init; }

    public string? Currency { get; init; }

    public string? TaxCode { get; init; }

    public string? QualityCompliance { get; init; }

    /// <summary>Percentage rates, not amounts.</summary>
    public decimal? Igst { get; init; }

    public decimal? Cgst { get; init; }

    public decimal? Sgst { get; init; }

    /// <summary>Free text alongside <see cref="IsActive"/> — "Blacklisted", "On hold".</summary>
    public string? ActiveStatus { get; init; }

    public bool IsActive { get; init; } = true;

    public MasterStatusDto Status { get; init; } = MasterStatusDto.Draft;
}

public sealed record CreateSupplierRequest : SaveSupplierRequest
{
    /// <summary>Business key. Unique per business unit, and not changed by an ordinary edit.</summary>
    public required string SupplierCode { get; init; }
}

public sealed record UpdateSupplierRequest : SaveSupplierRequest
{
    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}
