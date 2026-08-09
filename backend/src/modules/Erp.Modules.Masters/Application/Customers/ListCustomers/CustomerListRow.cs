using Erp.Persistence.Domain.Common;

namespace Erp.Modules.Masters.Application.Customers.ListCustomers;

/// <summary>
/// The shape the database query projects into, before it becomes a contract DTO.
/// See <c>SupplierListRow</c> for why the domain <see cref="MasterStatus"/> survives
/// this far and no further.
/// </summary>
internal sealed record CustomerListRow
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
