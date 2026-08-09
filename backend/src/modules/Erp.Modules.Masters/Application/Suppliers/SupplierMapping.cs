using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Modules.Masters.Domain.Common;
using Erp.Modules.Masters.Domain.Suppliers;

namespace Erp.Modules.Masters.Application.Suppliers;

/// <summary>
/// The one place a supplier's editable fields move between the wire and the entity.
/// <para>
/// Create and update share it, which is the point: two mappings drift, and the way
/// that shows up is a field you can set when adding a supplier and cannot change
/// afterwards. Normalisation happens here too, so a code is upper-cased whichever
/// direction it arrived from.
/// </para>
/// </summary>
internal static class SupplierMapping
{
    public static void Apply(Supplier supplier, SaveSupplierRequest request)
    {
        supplier.SupplierName = Normalize.Text(request.SupplierName);
        supplier.SupplierType = Normalize.Text(request.SupplierType);
        supplier.PrimaryContact = Normalize.Text(request.PrimaryContact);
        supplier.SecondaryContact = Normalize.Text(request.SecondaryContact);
        supplier.Phone = Normalize.Text(request.Phone);
        supplier.AltPhone = Normalize.Text(request.AltPhone);
        supplier.Email = Normalize.Text(request.Email);
        supplier.AltEmail = Normalize.Text(request.AltEmail);
        supplier.Website = Normalize.Text(request.Website);

        supplier.BillingAddress = Normalize.Text(request.BillingAddress);
        supplier.BillingCity = Normalize.Text(request.BillingCity);
        supplier.BillingState = Normalize.Text(request.BillingState);
        supplier.BillingCountry = Normalize.Text(request.BillingCountry);
        supplier.BillingZipCode = Normalize.Text(request.BillingZipCode);

        supplier.ShippingAddress = Normalize.Text(request.ShippingAddress);
        supplier.ShippingCity = Normalize.Text(request.ShippingCity);
        supplier.ShippingState = Normalize.Text(request.ShippingState);
        supplier.ShippingCountry = Normalize.Text(request.ShippingCountry);
        supplier.ShippingZipCode = Normalize.Text(request.ShippingZipCode);

        // Statutory identifiers are upper-case by definition — a PAN or GSTIN in
        // lower case is the same identifier and must not sort or match differently.
        supplier.Pan = Normalize.Code(request.Pan);
        supplier.TaxId = Normalize.Code(request.TaxId);
        supplier.GstNo = Normalize.Code(request.GstNo);

        supplier.BankName = Normalize.Text(request.BankName);
        supplier.AccountNumber = Normalize.Text(request.AccountNumber);
        supplier.Ifsc = Normalize.Code(request.Ifsc);
        supplier.Swift = Normalize.Code(request.Swift);

        supplier.PaymentTerms = Normalize.Text(request.PaymentTerms);
        supplier.Currency = Normalize.Code(request.Currency);
        supplier.TaxCode = Normalize.Code(request.TaxCode);
        supplier.QualityCompliance = Normalize.Text(request.QualityCompliance);

        supplier.Igst = request.Igst;
        supplier.Cgst = request.Cgst;
        supplier.Sgst = request.Sgst;

        supplier.ActiveStatus = Normalize.Text(request.ActiveStatus);
        supplier.IsActive = request.IsActive;
        supplier.Status = (MasterStatus)request.Status;
    }

    public static SupplierDetailDto ToDetail(Supplier supplier) => new()
    {
        Id = supplier.Id,
        SupplierCode = supplier.SupplierCode,
        SupplierName = supplier.SupplierName,
        SupplierType = supplier.SupplierType,
        PrimaryContact = supplier.PrimaryContact,
        SecondaryContact = supplier.SecondaryContact,
        Phone = supplier.Phone,
        AltPhone = supplier.AltPhone,
        Email = supplier.Email,
        AltEmail = supplier.AltEmail,
        Website = supplier.Website,
        BillingAddress = supplier.BillingAddress,
        BillingCity = supplier.BillingCity,
        BillingState = supplier.BillingState,
        BillingCountry = supplier.BillingCountry,
        BillingZipCode = supplier.BillingZipCode,
        ShippingAddress = supplier.ShippingAddress,
        ShippingCity = supplier.ShippingCity,
        ShippingState = supplier.ShippingState,
        ShippingCountry = supplier.ShippingCountry,
        ShippingZipCode = supplier.ShippingZipCode,
        Pan = supplier.Pan,
        TaxId = supplier.TaxId,
        GstNo = supplier.GstNo,
        BankName = supplier.BankName,
        AccountNumber = supplier.AccountNumber,
        Ifsc = supplier.Ifsc,
        Swift = supplier.Swift,
        PaymentTerms = supplier.PaymentTerms,
        Currency = supplier.Currency,
        TaxCode = supplier.TaxCode,
        QualityCompliance = supplier.QualityCompliance,
        Igst = supplier.Igst,
        Cgst = supplier.Cgst,
        Sgst = supplier.Sgst,
        ActiveStatus = supplier.ActiveStatus,
        IsActive = supplier.IsActive,
        Status = (MasterStatusDto)supplier.Status,
        BusinessUnitId = supplier.BusinessUnitId,
        CreatedAtUtc = supplier.CreatedAtUtc,
        ModifiedAtUtc = supplier.ModifiedAtUtc,
        RowVersion = Convert.ToBase64String(supplier.RowVersion),
    };
}
