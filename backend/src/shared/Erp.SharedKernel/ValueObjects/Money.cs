using System.Globalization;
using Erp.SharedKernel.Primitives;

namespace Erp.SharedKernel.ValueObjects;

/// <summary>
/// An amount and the currency it is denominated in.
/// <para>
/// Carrying the currency in the type is what makes a future multi-currency
/// requirement a compile error rather than a silent mis-total. The legacy system
/// hard-coded the rupee symbol into PDF templates and had no currency concept at
/// all, so adding an export customer would have meant auditing every arithmetic
/// site by hand.
/// </para>
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>ISO 4217 code used when a caller does not specify one.</summary>
    public const string DefaultCurrency = "INR";

    /// <summary>Scale of the <c>decimal(18,4)</c> column money is stored in.</summary>
    public const int Scale = 4;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>ISO 4217 alphabetic code, upper case.</summary>
    public string Currency { get; }

    public static Money Zero(string currency = DefaultCurrency) => Of(0m, currency);

    public static Money Of(decimal amount, string currency = DefaultCurrency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (currency.Length != 3)
        {
            throw new ArgumentException(
                $"'{currency}' is not an ISO 4217 alphabetic code.",
                nameof(currency));
        }

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Rounds to the stored scale, half away from zero — the convention Indian
    /// invoicing expects. Banker's rounding is deliberately not used here.
    /// </summary>
    public Money Round() =>
        new(decimal.Round(Amount, Scale, MidpointRounding.AwayFromZero), Currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator *(Money left, decimal right) => left.Multiply(right);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.####} {Currency}");

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // A currency mismatch is a programming error, not a business outcome,
        // so it throws rather than returning a Result.
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot combine {Currency} with {other.Currency}. Convert explicitly first.");
        }
    }
}
