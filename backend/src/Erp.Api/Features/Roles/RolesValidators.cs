using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.Roles;

public sealed class SaveRoleMasterValidator : AbstractValidator<SaveRoleMasterRequest>
{
    public SaveRoleMasterValidator()
    {
        RuleFor(x => x.RolesName)
            .NotEmpty().WithMessage("Role name is required.");

        this.MaxLength(x => x.RolesName, 100, "Role name");

        this.Pattern(
            x => x.RolesName,
            "^[0-9A-Za-z ]+$",
            "Role name may contain only letters, digits and spaces.");
    }
}

public sealed class CreateRoleMasterValidator : AbstractValidator<CreateRoleMasterRequest>
{
    public CreateRoleMasterValidator()
    {
        Include(new SaveRoleMasterValidator());

        RuleFor(x => x.RoleId)
            .GreaterThan(0)
            .WithMessage("Role id must be a positive number.");
    }
}

public sealed class UpdateRoleMasterValidator : AbstractValidator<UpdateRoleMasterRequest>
{
    public UpdateRoleMasterValidator()
    {
        Include(new SaveRoleMasterValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the role before updating it.");
    }
}
