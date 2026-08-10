using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.Assemblies;

public sealed class AssemblyNodeAttributesValidator : AbstractValidator<AssemblyNodeAttributesDto>
{
    public AssemblyNodeAttributesValidator()
    {
        this.MaxLength(x => x.ManualCode, 50, "Manual code");
        this.MaxLength(x => x.MachineType, 50, "Machine type");
        this.MaxLength(x => x.DrivenBy, 100, "Driven by");
        this.MaxLength(x => x.DrawingPath, 500, "Drawing path");
        this.MaxLength(x => x.TechnicalSpecification, 2500, "Technical specification");
        this.MaxLength(x => x.Remark, 500, "Remark");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(0m, 999_999_999m)
            .WithMessage("Quantity must be between 0 and 999,999,999.");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(0m, 9_999_999.9999m)
            .WithMessage("Weight must be between 0 and 9,999,999.9999 kg.");

        this.NonNegative(x => x.DisplaySequence, "Sequence");
    }
}

public sealed class CreateAssemblyNodeValidator : AbstractValidator<CreateAssemblyNodeRequest>
{
    public CreateAssemblyNodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        this.MaxLength(x => x.Code, 30, "Code");
        this.Pattern(
            x => x.Code,
            "^[A-Za-z0-9][A-Za-z0-9._/-]*$",
            "Code may contain only letters, digits, dot, underscore, slash and hyphen.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.");

        this.MaxLength(x => x.Name, 255, "Name");

        RuleFor(x => x.Attributes!)
            .SetValidator(new AssemblyNodeAttributesValidator())
            .When(x => x.Attributes is not null);
    }
}

public sealed class UpdateAssemblyNodeValidator : AbstractValidator<UpdateAssemblyNodeRequest>
{
    public UpdateAssemblyNodeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.");

        this.MaxLength(x => x.Name, 255, "Name");

        RuleFor(x => x.Attributes!)
            .SetValidator(new AssemblyNodeAttributesValidator())
            .When(x => x.Attributes is not null);

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the record before updating it.");
    }
}
