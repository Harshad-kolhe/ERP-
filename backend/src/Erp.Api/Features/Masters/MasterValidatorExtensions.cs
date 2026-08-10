using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentValidation;

namespace Erp.Api.Features.Masters;

/// <summary>
/// The rules that repeat across every master, written once.
/// <para>
/// Five masters carry a PAN, four carry a GSTIN, and all of them carry a dozen
/// length-bounded strings. Restating those inline produced, in the system this
/// replaces, four different opinions about how long an email address may be. Each
/// helper here is optional-friendly: a blank value passes, because "not supplied"
/// is not the same as "supplied and wrong", and only <c>NotEmpty</c> decides
/// whether absence is allowed.
/// </para>
/// </summary>
public static class MasterValidatorExtensions
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>Checks the trimmed length, because that is what gets stored.</summary>
    public static void MaxLength<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        int maximum,
        string label)
    {
        // Compiled once at construction. Compiling inside the When would put
        // expression-tree work on every field of every save.
        var read = selector.Compile();

        validator.RuleFor(selector)
            .Must(value => value!.Trim().Length <= maximum)
            .WithMessage($"{label} must be {maximum} characters or fewer.")
            .When(instance => !string.IsNullOrWhiteSpace(read(instance)));
    }

    public static void Pattern<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        [StringSyntax(StringSyntaxAttribute.Regex)] string pattern,
        string message)
    {
        var read = selector.Compile();
        var regex = new Regex(pattern, RegexOptions.None, RegexTimeout);

        validator.RuleFor(selector)
            .Must(value => regex.IsMatch(value!.Trim()))
            .WithMessage(message)
            .When(instance => !string.IsNullOrWhiteSpace(read(instance)));
    }

    /// <summary>
    /// Deliberately permissive: something, an @, something, a dot, something.
    /// <para>
    /// Stricter patterns reject addresses that work â€” plus-addressing, new top-level
    /// domains, apostrophes â€” and the only way to actually know an address is real is
    /// to send to it. This catches the typo where a phone number went in the email box.
    /// </para>
    /// </summary>
    public static void Email<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        string label)
    {
        validator.MaxLength(selector, 150, label);
        validator.Pattern(selector, @"^[^\s@]+@[^\s@]+\.[^\s@]+$", $"{label} is not a valid email address.");
    }

    /// <summary>Five letters, four digits, one letter â€” the Indian income tax PAN format.</summary>
    public static void Pan<T>(this AbstractValidator<T> validator, Expression<Func<T, string?>> selector)
    {
        validator.Pattern(
            selector,
            "^[A-Za-z]{5}[0-9]{4}[A-Za-z]$",
            "PAN must be 10 characters, e.g. AAAPA1234A.");
    }

    /// <summary>State code, PAN, entity number, Z, check character â€” the 15-character GSTIN.</summary>
    public static void Gstin<T>(this AbstractValidator<T> validator, Expression<Func<T, string?>> selector)
    {
        validator.Pattern(
            selector,
            "^[0-9]{2}[A-Za-z]{5}[0-9]{4}[A-Za-z][0-9A-Za-z][Zz][0-9A-Za-z]$",
            "GST number must be 15 characters, e.g. 27AAAPA1234A1Z5.");
    }

    /// <summary>
    /// A GST percentage, 0â€“100.
    /// <para>
    /// Bounded because the commonest mistake in these boxes is entering the tax
    /// <em>amount</em> instead of the rate. A stored 4,500% looks unremarkable in a
    /// grid cell and is noticed when an invoice is wrong.
    /// </para>
    /// </summary>
    public static void TaxRate<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, decimal?>> selector,
        string label)
    {
        validator.RuleFor(selector)
            .InclusiveBetween(0m, 100m)
            .WithMessage($"{label} must be a percentage between 0 and 100.")
            .When(_ => true);
    }

    /// <summary>A money amount that cannot be negative.</summary>
    public static void Money<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, decimal?>> selector,
        string label)
    {
        validator.RuleFor(selector)
            .InclusiveBetween(0m, 9_999_999_999.99m)
            .WithMessage($"{label} must be between 0 and 9,999,999,999.99.");
    }

    /// <summary>A count or measurement that cannot be negative.</summary>
    public static void NonNegative<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int?>> selector,
        string label)
    {
        validator.RuleFor(selector)
            .GreaterThanOrEqualTo(0)
            .WithMessage($"{label} cannot be negative.");
    }
}
