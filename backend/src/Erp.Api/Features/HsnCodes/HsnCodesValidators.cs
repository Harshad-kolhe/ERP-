using Erp.Api.Features.Masters;
using Erp.Contracts.Masters;
using FluentValidation;

namespace Erp.Api.Features.HsnCodes;

public sealed class SaveHsnCodeValidator : AbstractValidator<SaveHsnCodeRequest>
{
    public SaveHsnCodeValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        this.MaxLength(x => x.Description, 250, "Description");
    }
}

public static class HsnRateRules
{
    public static void AddTo<T>(AbstractValidator<T> validator, Func<T, decimal> rate)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(rate);

        validator.RuleFor(x => rate(x))
            .InclusiveBetween(0m, 100m)
            .WithName("ratePercent")
            .WithMessage("Rate must be between 0 and 100 percent.");
    }
}

public sealed class CreateHsnCodeValidator : AbstractValidator<CreateHsnCodeRequest>
{
    public CreateHsnCodeValidator()
    {
        Include(new SaveHsnCodeValidator());

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        this.Pattern(
            x => x.Code,
            "^[0-9]{4}([0-9]{2}([0-9]{2})?)?$",
            "HSN code must be 4, 6 or 8 digits.");

        HsnRateRules.AddTo(this, x => x.RatePercent);
    }
}

public sealed class UpdateHsnCodeValidator : AbstractValidator<UpdateHsnCodeRequest>
{
    public UpdateHsnCodeValidator()
    {
        Include(new SaveHsnCodeValidator());

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Row version is required. Re-read the code before updating it.");
    }
}

public sealed class AddHsnGstRateValidator : AbstractValidator<AddHsnGstRateRequest>
{
    public AddHsnGstRateValidator()
    {
        HsnRateRules.AddTo(this, x => x.RatePercent);
    }
}
