using Erp.Api.Common.Excel;

namespace Erp.Api.Features.Suppliers;

/// <summary>
/// Every column of the suppliers import sheet, matching the grid's captions and
/// the column lengths in <c>SupplierConfiguration</c>.
/// </summary>
public static class SupplierImportColumns
{
    public static readonly ImportColumn SupplierCode = new(
        "Supplier code",
        Required: true,
        MaxLength: 50,
        Note: "The supplier's business key. Must not already exist.");

    public static readonly ImportColumn SupplierName = new("Supplier name", Required: true, MaxLength: 200);

    public static readonly ImportColumn SupplierType = new("Supplier type", MaxLength: 50);

    public static readonly ImportColumn PrimaryContact = new("Primary contact", MaxLength: 100);

    public static readonly ImportColumn SecondaryContact = new("Secondary contact", MaxLength: 100);

    public static readonly ImportColumn Phone = new("Phone", MaxLength: 30);

    public static readonly ImportColumn AltPhone = new("Alt phone", MaxLength: 30);

    public static readonly ImportColumn Email = new("Email", MaxLength: 150);

    public static readonly ImportColumn AltEmail = new("Alt email", MaxLength: 150);

    public static readonly ImportColumn Website = new("Website", MaxLength: 200);

    public static readonly ImportColumn BillingAddress = new("Billing address", MaxLength: 500);

    public static readonly ImportColumn BillingCountry = new("Billing country", MaxLength: 100);

    public static readonly ImportColumn BillingState = new("Billing state", MaxLength: 100);

    public static readonly ImportColumn BillingCity = new("Billing city", MaxLength: 100);

    public static readonly ImportColumn BillingZipCode = new("Billing zipcode", MaxLength: 20);

    public static readonly ImportColumn ShippingAddress = new("Shipping address", MaxLength: 500);

    public static readonly ImportColumn ShippingCountry = new("Shipping country", MaxLength: 100);

    public static readonly ImportColumn ShippingState = new("Shipping state", MaxLength: 100);

    public static readonly ImportColumn ShippingCity = new("Shipping city", MaxLength: 100);

    public static readonly ImportColumn ShippingZipCode = new("Shipping zipcode", MaxLength: 20);

    public static readonly ImportColumn Pan = new("PAN", MaxLength: 10);

    public static readonly ImportColumn TaxId = new("Tax ID", MaxLength: 50);

    public static readonly ImportColumn GstNo = new("GST no", MaxLength: 15);

    public static readonly ImportColumn BankName = new("Bank name", MaxLength: 150);

    public static readonly ImportColumn AccountNumber = new("Account number", MaxLength: 50);

    public static readonly ImportColumn Ifsc = new("IFSC", MaxLength: 11);

    public static readonly ImportColumn Swift = new("SWIFT", MaxLength: 11);

    public static readonly ImportColumn PaymentTerms = new("Payment terms", MaxLength: 100);

    public static readonly ImportColumn Currency = new("Currency", MaxLength: 3, Note: "INR, USD, EUR, GBPâ€¦");

    public static readonly ImportColumn TaxCode = new("Tax code", MaxLength: 50);

    public static readonly ImportColumn QualityCompliance = new("Quality compliance", MaxLength: 200);

    public static readonly ImportColumn Igst = new("IGST", ImportColumnKind.Number, Note: "Percentage rate, not an amount.");

    public static readonly ImportColumn Cgst = new("CGST", ImportColumnKind.Number, Note: "Percentage rate, not an amount.");

    public static readonly ImportColumn Sgst = new("SGST", ImportColumnKind.Number, Note: "Percentage rate, not an amount.");

    public static readonly ImportColumn ActiveStatus = new(
        "Active status",
        MaxLength: 50,
        Note: "Free text carried from the legacy system, e.g. Blacklisted or On hold.");

    public static readonly ImportColumn IsActive = new(
        "Active",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as Yes.");

    public static readonly ImportColumn Status = new(
        "Status",
        Note: "Draft, PendingApproval, Approved, Rejected or Hold. Blank counts as Draft.");

    public static readonly IReadOnlyList<ImportColumn> All =
    [
        SupplierCode,
        SupplierName,
        SupplierType,
        PrimaryContact,
        SecondaryContact,
        Phone,
        AltPhone,
        Email,
        AltEmail,
        Website,
        BillingAddress,
        BillingCountry,
        BillingState,
        BillingCity,
        BillingZipCode,
        ShippingAddress,
        ShippingCountry,
        ShippingState,
        ShippingCity,
        ShippingZipCode,
        Pan,
        TaxId,
        GstNo,
        BankName,
        AccountNumber,
        Ifsc,
        Swift,
        PaymentTerms,
        Currency,
        TaxCode,
        QualityCompliance,
        Igst,
        Cgst,
        Sgst,
        ActiveStatus,
        IsActive,
        Status,
    ];
}
