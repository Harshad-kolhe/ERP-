using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.Employees;

public sealed class SaveEmployeeValidator : AbstractValidator<SaveEmployeeRequest>
{
    public SaveEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.");

        this.MaxLength(x => x.FirstName, 100, "First name");
        this.MaxLength(x => x.MiddleName, 100, "Middle name");
        this.MaxLength(x => x.LastName, 100, "Last name");
        this.MaxLength(x => x.Gender, 20, "Gender");
        this.MaxLength(x => x.Address, 500, "Employee address");
        this.MaxLength(x => x.State, 100, "State");
        this.MaxLength(x => x.UserName, 100, "User name");
        this.MaxLength(x => x.Department, 100, "Department");
        this.MaxLength(x => x.Designation, 100, "Designation");
        this.MaxLength(x => x.PhoneNo, 30, "Phone no");
        this.MaxLength(x => x.BloodGroup, 10, "Blood group");
        this.MaxLength(x => x.Qualification, 200, "Qualification");
        this.MaxLength(x => x.PassportNo, 20, "Passport no.");

        this.Email(x => x.Email, "Email");

        this.Pattern(x => x.AadharNo, "^[0-9]{12}$", "Aadhar card no. must be 12 digits.");
        this.Pan(x => x.PanNo);

        this.NonNegative(x => x.ShoeSize, "Shoe size");

        this.Money(x => x.ProvidentFund, "Provident fund");
        this.Money(x => x.EmployeeStateInsurance, "Employee state insurance");
        this.Money(x => x.ProfessionalTax, "Professional tax");
        this.Money(x => x.IncomeTaxTds, "Income tax");
        this.Money(x => x.GrossSalary, "Gross salary");
        this.Money(x => x.NetSalary, "Net salary");
        this.Money(x => x.PerHourSalary, "Per hour salary");

        RuleFor(x => x.NetSalary)
            .LessThanOrEqualTo(x => x.GrossSalary)
            .WithMessage("Net salary cannot be more than gross salary.")
            .When(x => x.NetSalary.HasValue && x.GrossSalary.HasValue);

        RuleFor(x => x.DateOfBirth)
            .LessThan(x => x.JoiningDate)
            .WithMessage("Date of birth must be before the date of joining.")
            .When(x => x.DateOfBirth.HasValue && x.JoiningDate.HasValue);

        RuleFor(x => x.Skills)
            .Must(skills => skills.Count <= 50)
            .WithMessage("An employee can carry at most 50 skills.");

        RuleFor(x => x.Strengths)
            .Must(strengths => strengths.Count <= 50)
            .WithMessage("An employee can carry at most 50 strengths.");

        RuleFor(x => x.Status).IsInEnum().WithMessage("Status is not a known value.");
    }
}

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeValidator()
    {
        Include(new SaveEmployeeValidator());

        RuleFor(x => x.EmployeeCode)
            .GreaterThan(0)
            .WithMessage("Employee code must be a positive number.");
    }
}

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeValidator()
    {
        Include(new SaveEmployeeValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the employee before updating it.");
    }
}
