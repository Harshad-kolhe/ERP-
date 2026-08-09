using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Masters;
using FluentValidation;

namespace Erp.Modules.Masters.Application.Customers.WriteCustomer;

/// <summary>Rules for a customer's editable fields. Lengths mirror <c>CustomerConfiguration</c>.</summary>
internal sealed class SaveCustomerValidator : AbstractValidator<SaveCustomerRequest>
{
    public SaveCustomerValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.");

        this.MaxLength(x => x.CustomerName, 200, "Customer name");
        this.MaxLength(x => x.Industry, 100, "Industry");
        this.MaxLength(x => x.PrimaryContact, 100, "Primary contact person");
        this.MaxLength(x => x.SecondaryContact, 100, "Secondary contact person");
        this.MaxLength(x => x.Phone, 30, "Phone");
        this.MaxLength(x => x.AltPhone, 30, "Alt phone");
        this.MaxLength(x => x.Website, 200, "Website");
        this.MaxLength(x => x.BillingAddress, 500, "Billing address");
        this.MaxLength(x => x.BillingCity, 100, "Billing city");
        this.MaxLength(x => x.BillingState, 100, "Billing state");
        this.MaxLength(x => x.BillingCountry, 100, "Billing country");
        this.MaxLength(x => x.BillingZipCode, 20, "Billing zip code");
        this.MaxLength(x => x.ShippingAddress, 500, "Shipping address");
        this.MaxLength(x => x.ShippingCity, 100, "Shipping city");
        this.MaxLength(x => x.ShippingState, 100, "Shipping state");
        this.MaxLength(x => x.ShippingCountry, 100, "Shipping country");
        this.MaxLength(x => x.ShippingZipCode, 20, "Shipping zip code");
        this.MaxLength(x => x.TaxId, 50, "Tax id");
        this.MaxLength(x => x.Currency, 3, "Currency");
        this.MaxLength(x => x.TaxCode, 50, "Tax code");

        this.Email(x => x.Email, "Email");
        this.Email(x => x.AltEmail, "Alt email");

        this.Gstin(x => x.Gst);
        this.Pan(x => x.Pan);

        this.TaxRate(x => x.Igst, "IGST");
        this.TaxRate(x => x.Cgst, "CGST");
        this.TaxRate(x => x.Sgst, "SGST");

        RuleFor(x => x.Status).IsInEnum().WithMessage("Status is not a known value.");
    }
}

internal sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        Include(new SaveCustomerValidator());

        RuleFor(x => x.CustomerCode)
            .NotEmpty().WithMessage("Customer code is required.");

        this.MaxLength(x => x.CustomerCode, 50, "Customer code");
        this.Pattern(
            x => x.CustomerCode,
            "^[A-Za-z0-9][A-Za-z0-9._/-]*$",
            "Customer code may contain only letters, digits, dot, underscore, slash and hyphen.");
    }
}

internal sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        Include(new SaveCustomerValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the customer before updating it.");
    }
}
