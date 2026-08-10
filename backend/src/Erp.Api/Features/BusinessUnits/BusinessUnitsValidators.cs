using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.BusinessUnits;

public sealed class SaveBusinessUnitValidator : AbstractValidator<SaveBusinessUnitRequest>
{
    public SaveBusinessUnitValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.");

        this.MaxLength(x => x.BusinessName, 200, "Business name");
        this.MaxLength(x => x.Address, 500, "Address");
        this.MaxLength(x => x.StateName, 100, "State name");
        this.MaxLength(x => x.StateCode, 10, "State code");
        this.MaxLength(x => x.ContactNumber, 30, "Contact number");
        this.MaxLength(x => x.Website, 200, "Website");

        this.Email(x => x.Email, "Email");
        this.Gstin(x => x.Gstn);
        this.Pan(x => x.Pan);

        this.Pattern(x => x.Cin, "^[A-Za-z0-9]{21}$", "CIN must be 21 characters.");
    }
}

public sealed class CreateBusinessUnitValidator : AbstractValidator<CreateBusinessUnitRequest>
{
    public CreateBusinessUnitValidator()
    {
        Include(new SaveBusinessUnitValidator());

        RuleFor(x => x.BusinessUnitId)
            .GreaterThan(0)
            .WithMessage("Unit id must be a positive number.");
    }
}

public sealed class UpdateBusinessUnitValidator : AbstractValidator<UpdateBusinessUnitRequest>
{
    public UpdateBusinessUnitValidator()
    {
        Include(new SaveBusinessUnitValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the business unit before updating it.");
    }
}
