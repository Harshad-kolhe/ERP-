using Erp.Api.Common.Security;
using Erp.Contracts.Security;
using FluentValidation;

namespace Erp.Api.Administration;

/// <summary>
/// Rejects permission codes the system does not define.
/// <para>
/// Without this an administrator could grant <c>masters.part.aprove</c> â€” a typo that
/// grants nothing, fails silently, and is then reported as "permissions don't work".
/// The catalogue makes an unknown code a validation error naming the offending value.
/// </para>
/// </summary>
internal sealed class CreateRoleValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleValidator(IPermissionCatalogue catalogue)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(250);

        RuleForEach(x => x.Permissions)
            .Must(catalogue.IsDefined)
            .WithMessage((_, code) => $"'{code}' is not a permission this system defines.");
    }
}

internal sealed class UpdateRoleValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleValidator(IPermissionCatalogue catalogue)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(250);

        RuleForEach(x => x.Permissions)
            .Must(catalogue.IsDefined)
            .WithMessage((_, code) => $"'{code}' is not a permission this system defines.");
    }
}
