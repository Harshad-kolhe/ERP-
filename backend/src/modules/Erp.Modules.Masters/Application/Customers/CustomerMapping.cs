using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using Erp.Persistence.Domain.Common;
using Erp.Persistence.Domain.Customers;

namespace Erp.Modules.Masters.Application.Customers;

/// <summary>The one place a customer's editable fields move between the wire and the entity.</summary>
internal static class CustomerMapping
{
    public static void Apply(Customer customer, SaveCustomerRequest request)
    {
        customer.CustomerName = Normalize.Text(request.CustomerName);
        customer.Industry = Normalize.Text(request.Industry);
        customer.PrimaryContact = Normalize.Text(request.PrimaryContact);
        customer.SecondaryContact = Normalize.Text(request.SecondaryContact);
        customer.Phone = Normalize.Text(request.Phone);
        customer.AltPhone = Normalize.Text(request.AltPhone);
        customer.Email = Normalize.Text(request.Email);
        customer.AltEmail = Normalize.Text(request.AltEmail);
        customer.Website = Normalize.Text(request.Website);

        customer.BillingAddress = Normalize.Text(request.BillingAddress);
        customer.BillingCity = Normalize.Text(request.BillingCity);
        customer.BillingState = Normalize.Text(request.BillingState);
        customer.BillingCountry = Normalize.Text(request.BillingCountry);
        customer.BillingZipCode = Normalize.Text(request.BillingZipCode);

        customer.ShippingAddress = Normalize.Text(request.ShippingAddress);
        customer.ShippingCity = Normalize.Text(request.ShippingCity);
        customer.ShippingState = Normalize.Text(request.ShippingState);
        customer.ShippingCountry = Normalize.Text(request.ShippingCountry);
        customer.ShippingZipCode = Normalize.Text(request.ShippingZipCode);

        // Statutory identifiers are upper-case by definition.
        customer.TaxId = Normalize.Code(request.TaxId);
        customer.Gst = Normalize.Code(request.Gst);
        customer.Pan = Normalize.Code(request.Pan);

        customer.Igst = request.Igst;
        customer.Cgst = request.Cgst;
        customer.Sgst = request.Sgst;

        customer.Currency = Normalize.Code(request.Currency);
        customer.TaxCode = Normalize.Code(request.TaxCode);
        customer.IsActive = request.IsActive;
        customer.Status = (MasterStatus)request.Status;
    }

    public static CustomerDetailDto ToDetail(Customer customer) => new()
    {
        Id = customer.Id,
        CustomerCode = customer.CustomerCode,
        CustomerName = customer.CustomerName,
        Industry = customer.Industry,
        PrimaryContact = customer.PrimaryContact,
        SecondaryContact = customer.SecondaryContact,
        Phone = customer.Phone,
        AltPhone = customer.AltPhone,
        Email = customer.Email,
        AltEmail = customer.AltEmail,
        Website = customer.Website,
        BillingAddress = customer.BillingAddress,
        BillingCity = customer.BillingCity,
        BillingState = customer.BillingState,
        BillingCountry = customer.BillingCountry,
        BillingZipCode = customer.BillingZipCode,
        ShippingAddress = customer.ShippingAddress,
        ShippingCity = customer.ShippingCity,
        ShippingState = customer.ShippingState,
        ShippingCountry = customer.ShippingCountry,
        ShippingZipCode = customer.ShippingZipCode,
        TaxId = customer.TaxId,
        Gst = customer.Gst,
        Pan = customer.Pan,
        Igst = customer.Igst,
        Cgst = customer.Cgst,
        Sgst = customer.Sgst,
        Currency = customer.Currency,
        TaxCode = customer.TaxCode,
        IsActive = customer.IsActive,
        Status = (MasterStatusDto)customer.Status,
        BusinessUnitId = customer.BusinessUnitId,
        CreatedAtUtc = customer.CreatedAtUtc,
        ModifiedAtUtc = customer.ModifiedAtUtc,
        RowVersion = Convert.ToBase64String(customer.RowVersion),
    };
}
