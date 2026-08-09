namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the customers grid. See <see cref="SupplierListItemDto"/> for why a
/// list row this wide is the right call for a ported master screen.
/// </summary>
public sealed record CustomerListItemDto
{
    public required int Id { get; init; }

    public required string? CustomerCode { get; init; }

    public required string? CustomerName { get; init; }

    public required string? Industry { get; init; }

    /// <summary>Legacy "Primary Contact Person".</summary>
    public string? PrimaryContact { get; init; }

    public string? SecondaryContact { get; init; }

    public required string? Phone { get; init; }

    public string? AltPhone { get; init; }

    public required string? Email { get; init; }

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

    public required string? Gst { get; init; }

    public string? Pan { get; init; }

    /// <summary>GST percentage rates, not amounts.</summary>
    public decimal? Igst { get; init; }

    public decimal? Cgst { get; init; }

    public decimal? Sgst { get; init; }

    public string? Currency { get; init; }

    public string? TaxCode { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatusDto Status { get; init; }

    /// <summary>Display name of the author, resolved server-side. Null if that user no longer exists.</summary>
    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public string? ModifiedBy { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
