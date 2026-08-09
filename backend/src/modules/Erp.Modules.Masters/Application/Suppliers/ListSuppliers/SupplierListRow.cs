using Erp.Modules.Masters.Domain.Common;

namespace Erp.Modules.Masters.Application.Suppliers.ListSuppliers;

/// <summary>
/// The shape the database query projects into, before it becomes a contract DTO.
/// <para>
/// It exists so sorting and filtering happen against the domain
/// <see cref="MasterStatus"/> — which EF translates through its value converter —
/// while <c>Erp.Contracts</c> stays free of any domain type.
/// </para>
/// </summary>
internal sealed record SupplierListRow
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
