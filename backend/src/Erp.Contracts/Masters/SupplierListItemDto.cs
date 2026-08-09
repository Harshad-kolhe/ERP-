namespace Erp.Contracts.Masters;

/// <summary>
/// One row of the suppliers grid.
/// <para>
/// Wide, because the legacy Supplier Master grid is: purchasing staff work across
/// the contact block, both addresses, the tax block and the bank block from this
/// one screen, so a narrower row would only push them back to the old system. Most
/// of it arrives hidden and is turned on from the column chooser.
/// </para>
/// <para>
/// It is still a projection — the handler never loads a <c>Supplier</c> aggregate,
/// and the database still does the filtering, sorting, counting and paging.
/// </para>
/// </summary>
public sealed record SupplierListItemDto
{
    public required int Id { get; init; }

    public required string? SupplierCode { get; init; }

    public required string? SupplierName { get; init; }

    public required string? SupplierType { get; init; }

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

    public string? Pan { get; init; }

    public string? TaxId { get; init; }

    public required string? GstNo { get; init; }

    public string? BankName { get; init; }

    /// <summary>
    /// Shown in full, as the legacy grid did. It is a payee reference on documents
    /// this system prints, not a credential — masking it would stop purchasing
    /// checking a remittance against the supplier record.
    /// </summary>
    public string? AccountNumber { get; init; }

    public string? Ifsc { get; init; }

    public string? Swift { get; init; }

    public string? PaymentTerms { get; init; }

    public string? Currency { get; init; }

    public string? TaxCode { get; init; }

    public string? QualityCompliance { get; init; }

    /// <summary>GST percentage rates, not amounts.</summary>
    public decimal? Igst { get; init; }

    public decimal? Cgst { get; init; }

    public decimal? Sgst { get; init; }

    /// <summary>
    /// The legacy free-text "Active Status". Kept because migrated rows carry values
    /// the boolean cannot express ("Blacklisted", "On hold"), and losing them would
    /// lose the reason a supplier is not being bought from.
    /// </summary>
    public string? ActiveStatus { get; init; }

    public required bool IsActive { get; init; }

    public required MasterStatusDto Status { get; init; }

    /// <summary>Display name of the author, resolved server-side. Null if that user no longer exists.</summary>
    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public string? ModifiedBy { get; init; }

    public DateTimeOffset? ModifiedAtUtc { get; init; }
}
