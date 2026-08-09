using Erp.BuildingBlocks.Excel;

namespace Erp.Modules.Masters.Application.Customers.ImportCustomers;

/// <summary>
/// Every column of the customers import sheet, matching the grid's captions and
/// the column lengths in <c>CustomerConfiguration</c>.
/// </summary>
internal static class CustomerImportColumns
{
    public static readonly ImportColumn CustomerCode = new(
        "Customer code",
        Required: true,
        MaxLength: 50,
        Note: "The customer's business key. Must not already exist.");

    public static readonly ImportColumn CustomerName = new("Customer name", Required: true, MaxLength: 200);

    public static readonly ImportColumn Industry = new("Industry", MaxLength: 100);

    public static readonly ImportColumn PrimaryContact = new("Primary contact person", MaxLength: 100);

    public static readonly ImportColumn SecondaryContact = new("Secondary contact person", MaxLength: 100);

    public static readonly ImportColumn Phone = new("Phone", MaxLength: 30);

    public static readonly ImportColumn AltPhone = new("Alt phone", MaxLength: 30);

    public static readonly ImportColumn Email = new("Email", MaxLength: 150);

    public static readonly ImportColumn AltEmail = new("Alt email", MaxLength: 150);

    public static readonly ImportColumn Website = new("Website", MaxLength: 200);

    public static readonly ImportColumn BillingAddress = new("Billing address", MaxLength: 500);

    public static readonly ImportColumn BillingCountry = new("Billing country", MaxLength: 100);

    public static readonly ImportColumn BillingState = new("Billing state", MaxLength: 100);

    public static readonly ImportColumn BillingCity = new("Billing city", MaxLength: 100);

    public static readonly ImportColumn BillingZipCode = new("Billing zip code", MaxLength: 20);

    public static readonly ImportColumn ShippingAddress = new("Shipping address", MaxLength: 500);

    public static readonly ImportColumn ShippingCountry = new("Shipping country", MaxLength: 100);

    public static readonly ImportColumn ShippingState = new("Shipping state", MaxLength: 100);

    public static readonly ImportColumn ShippingCity = new("Shipping city", MaxLength: 100);

    public static readonly ImportColumn ShippingZipCode = new("Shipping zip code", MaxLength: 20);

    public static readonly ImportColumn TaxId = new("Tax id", MaxLength: 50);

    public static readonly ImportColumn Gst = new("GST", MaxLength: 15);

    public static readonly ImportColumn Pan = new("PAN", MaxLength: 10);

    public static readonly ImportColumn Igst = new("IGST", ImportColumnKind.Number, Note: "Percentage rate, not an amount.");

    public static readonly ImportColumn Cgst = new("CGST", ImportColumnKind.Number, Note: "Percentage rate, not an amount.");

    public static readonly ImportColumn Sgst = new("SGST", ImportColumnKind.Number, Note: "Percentage rate, not an amount.");

    public static readonly ImportColumn Currency = new("Currency", MaxLength: 3, Note: "INR, USD, EUR, GBP…");

    public static readonly ImportColumn TaxCode = new("Tax code", MaxLength: 50);

    public static readonly ImportColumn IsActive = new(
        "Active",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as Yes.");

    public static readonly ImportColumn Status = new(
        "Status",
        Note: "Draft, PendingApproval, Approved, Rejected or Hold. Blank counts as Draft.");

    public static readonly IReadOnlyList<ImportColumn> All =
    [
        CustomerCode,
        CustomerName,
        Industry,
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
        TaxId,
        Gst,
        Pan,
        Igst,
        Cgst,
        Sgst,
        Currency,
        TaxCode,
        IsActive,
        Status,
    ];
}
