namespace Erp.Contracts.Masters;

/// <summary>
/// One supplier, as the edit screen loads it.
/// <para>
/// Deliberately the same field names as <see cref="SaveSupplierRequest"/>, so the
/// form fills itself from this and posts the same shape back. A detail response
/// whose names differ from the request's is how an edit screen silently drops a
/// field that nobody notices until a purchase order prints without it.
/// </para>
/// </summary>
public sealed record SupplierDetailDto
{
    public required int Id { get; init; }

    public required string? SupplierCode { get; init; }

    public required string? SupplierName { get; init; }

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

    public decimal? Igst { get; init; }

    public decimal? Cgst { get; init; }

    public decimal? Sgst { get; init; }

    public string? ActiveStatus { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatusDto Status { get; init; }

    public required int BusinessUnitId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }

    /// <summary>Send back unchanged on update; a stale value yields 409.</summary>
    public required string RowVersion { get; init; }
}
