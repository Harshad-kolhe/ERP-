using Erp.Persistence.Domain.Common;
using Erp.SharedKernel.Primitives;

namespace Erp.Persistence.Domain.Customers;

/// <summary>
/// A customer master record, ported field-for-field from the legacy
/// <c>CustomerMaster</c>.
/// <para>
/// The legacy class mapped almost every property to a differently-cased column
/// through <c>[Column]</c> attributes — <c>customerCode</c>, <c>billing_state</c>,
/// <c>shipping_zipcode</c> — because the table was built by hand before the entity
/// was. This is a new table, so the names agree with the properties and the
/// attributes are gone.
/// </para>
/// <para>
/// Audit stamps come from <see cref="IAuditable"/>; see <c>Supplier</c> for why the
/// legacy <c>CreatedBy</c>/<c>ModifiedOn</c> columns are not carried across.
/// </para>
/// </summary>
public sealed class Customer : AggregateRoot<int>, IAuditable, ISoftDeletable, IBusinessUnitScoped, IHasRowVersion
{
    /// <summary>Business key. Unique per business unit.</summary>
    public string? CustomerCode { get; set; }

    public string? CustomerName { get; set; }

    public string? Industry { get; set; }

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

    public string? TaxId { get; set; }

    public string? Gst { get; set; }

    public string? Pan { get; set; }

    public string? Currency { get; set; }

    public string? TaxCode { get; set; }

    /// <summary>Percentage rates, not amounts — hence the (9,4) precision in the configuration.</summary>
    public decimal? Igst { get; set; }

    public decimal? Cgst { get; set; }

    public decimal? Sgst { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Legacy display ordinal. Carried over; the grid does not read it.</summary>
    public int? SrNo { get; set; }

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
