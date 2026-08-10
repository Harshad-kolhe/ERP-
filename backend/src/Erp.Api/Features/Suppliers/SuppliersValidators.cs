using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.Suppliers;

public sealed class SaveSupplierValidator : AbstractValidator<SaveSupplierRequest>
{
    public SaveSupplierValidator()
    {
        RuleFor(x => x.SupplierName)
            .NotEmpty().WithMessage("Supplier name is required.");

        this.MaxLength(x => x.SupplierName, 200, "Supplier name");
        this.MaxLength(x => x.SupplierType, 50, "Supplier type");
        this.MaxLength(x => x.PrimaryContact, 100, "Primary contact");
        this.MaxLength(x => x.SecondaryContact, 100, "Secondary contact");
        this.MaxLength(x => x.Phone, 30, "Phone");
        this.MaxLength(x => x.AltPhone, 30, "Alt phone");
        this.MaxLength(x => x.Website, 200, "Website");
        this.MaxLength(x => x.BillingAddress, 500, "Billing address");
        this.MaxLength(x => x.BillingCity, 100, "Billing city");
        this.MaxLength(x => x.BillingState, 100, "Billing state");
        this.MaxLength(x => x.BillingCountry, 100, "Billing country");
        this.MaxLength(x => x.BillingZipCode, 20, "Billing zipcode");
        this.MaxLength(x => x.ShippingAddress, 500, "Shipping address");
        this.MaxLength(x => x.ShippingCity, 100, "Shipping city");
        this.MaxLength(x => x.ShippingState, 100, "Shipping state");
        this.MaxLength(x => x.ShippingCountry, 100, "Shipping country");
        this.MaxLength(x => x.ShippingZipCode, 20, "Shipping zipcode");
        this.MaxLength(x => x.TaxId, 50, "Tax ID");
        this.MaxLength(x => x.BankName, 150, "Bank name");
        this.MaxLength(x => x.AccountNumber, 50, "Account number");
        this.MaxLength(x => x.PaymentTerms, 100, "Payment terms");
        this.MaxLength(x => x.Currency, 3, "Currency");
        this.MaxLength(x => x.TaxCode, 50, "Tax code");
        this.MaxLength(x => x.QualityCompliance, 200, "Quality compliance");
        this.MaxLength(x => x.ActiveStatus, 50, "Active status");

        this.Email(x => x.Email, "Email");
        this.Email(x => x.AltEmail, "Alt email");

        this.Pan(x => x.Pan);
        this.Gstin(x => x.GstNo);

        this.Pattern(x => x.Ifsc, "^[A-Za-z]{4}0[A-Za-z0-9]{6}$", "IFSC must be 11 characters, e.g. HDFC0001234.");
        this.Pattern(x => x.Swift, "^[A-Za-z]{6}[A-Za-z0-9]{2}([A-Za-z0-9]{3})?$", "SWIFT must be 8 or 11 characters.");

        this.TaxRate(x => x.Igst, "IGST");
        this.TaxRate(x => x.Cgst, "CGST");
        this.TaxRate(x => x.Sgst, "SGST");

        RuleFor(x => x.Status).IsInEnum().WithMessage("Status is not a known value.");
    }
}

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierValidator()
    {
        Include(new SaveSupplierValidator());

        RuleFor(x => x.SupplierCode)
            .NotEmpty().WithMessage("Supplier code is required.");

        this.MaxLength(x => x.SupplierCode, 50, "Supplier code");
        this.Pattern(
            x => x.SupplierCode,
            "^[A-Za-z0-9][A-Za-z0-9._/-]*$",
            "Supplier code may contain only letters, digits, dot, underscore, slash and hyphen.");
    }
}

public sealed class UpdateSupplierValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierValidator()
    {
        Include(new SaveSupplierValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the supplier before updating it.");
    }
}
