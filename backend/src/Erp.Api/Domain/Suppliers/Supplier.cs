using Erp.Api.Domain.Common;
using Erp.Api.Common.Entities;

namespace Erp.Api.Domain.Suppliers;

/// <summary>
/// A supplier master record, ported field-for-field from the legacy
/// <c>SupplierMaster</c> so migrated rows map across without a translation step.
/// <para>
/// The key is a plain <see cref="int"/> identity rather than the time-ordered Guid
/// used by <c>Part</c>: these tables receive legacy data keyed by int, and a master
/// table takes thousands of rows, not the millions a year where Guid index
/// fragmentation starts to matter.
/// </para>
/// <para>
/// The legacy <c>CreatedBy</c>/<c>CreatedOn</c>/<c>ModifiedBy</c>/<c>ModifiedOn</c>
/// columns are deliberately absent: <see cref="IAuditable"/> supplies the same four
/// stamps and they are written by an interceptor, so carrying both would leave two
/// sets of audit columns that disagree the moment anyone forgot to set one.
/// </para>
/// <para>
/// Properties are settable because the create and edit slices are not built yet.
/// When they arrive, the transitions move onto this class as methods, as they are
/// on <c>Part</c>.
/// </para>
/// </summary>
public sealed class Supplier : AggregateRoot<int>, IAuditable, ISoftDeletable, IBusinessUnitScoped, IHasRowVersion
{
    /// <summary>Business key. Unique per business unit.</summary>
    public string? SupplierCode { get; set; }

    public string? SupplierName { get; set; }

    public string? SupplierType { get; set; }

    /// <summary>Legacy spelling was <c>SupplierCatlog</c>; the typo is not carried forward.</summary>
    public string? SupplierCatalog { get; set; }

    public string? PrimaryContact { get; set; }

    public string? SecondaryContact { get; set; }

    public string? Phone { get; set; }

    public string? AltPhone { get; set; }

    public string? Email { get; set; }

    public string? AltEmail { get; set; }

    public string? Website { get; set; }

    public string? BillingAddress { get; set; }

    public string? BillingCity { get; set; }

    public string? BillingState { get; set; }

    public string? BillingCountry { get; set; }

    public string? BillingZipCode { get; set; }

    public string? ShippingAddress { get; set; }

    public string? ShippingCity { get; set; }

    public string? ShippingState { get; set; }

    public string? ShippingCountry { get; set; }

    public string? ShippingZipCode { get; set; }

    public string? Pan { get; set; }

    public string? TaxId { get; set; }

    public string? GstNo { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }

    public string? Ifsc { get; set; }

    public string? Swift { get; set; }

    public string? PaymentTerms { get; set; }

    public string? Currency { get; set; }

    public string? TaxCode { get; set; }

    public string? QualityCompliance { get; set; }

    public DateTimeOffset? ContractStartDate { get; set; }

    public DateTimeOffset? ContractEndDate { get; set; }

    public bool IsContracted { get; set; }

    /// <summary>Percentage rates, not amounts â€” hence the (9,4) precision in the configuration.</summary>
    public decimal? Igst { get; set; }

    public decimal? Cgst { get; set; }

    public decimal? Sgst { get; set; }

    /// <summary>Legacy <c>ActiveStatus</c>, kept as free text alongside <see cref="IsActive"/>.</summary>
    public string? ActiveStatus { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Legacy display ordinal. Carried over; the grid does not read it.</summary>
    public int? SrNo { get; set; }

    /// <summary>Legacy provenance marker recording which screen wrote the row.</summary>
    public string? ProgramId { get; set; }

    public MasterStatus Status { get; set; } = MasterStatus.Draft;

    public int BusinessUnitId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
