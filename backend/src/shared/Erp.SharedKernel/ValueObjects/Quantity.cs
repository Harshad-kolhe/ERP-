using System.Globalization;
using Erp.SharedKernel.Primitives;

namespace Erp.SharedKernel.ValueObjects;

/// <summary>
/// An amount of something, together with the unit it is measured in.
/// <para>
/// Stock is stored at <c>decimal(18,6)</c> — six places, not four — because
/// conversion factors (metres per kilogram of film, pieces per box) compound,
/// and rounding at four places drifts visibly across a year of transactions.
/// </para>
/// </summary>
public sealed class Quantity : ValueObject
{
    /// <summary>Scale of the <c>decimal(18,6)</c> column quantities are stored in.</summary>
    public const int Scale = 6;

    private Quantity(decimal value, string unitOfMeasureCode)
    {
        Value = value;
        UnitOfMeasureCode = unitOfMeasureCode;
    }

    public decimal Value { get; }

    public string UnitOfMeasureCode { get; }

    public bool IsZero => Value == 0m;

    public static Quantity Zero(string unitOfMeasureCode) => Of(0m, unitOfMeasureCode);

    public static Quantity Of(decimal value, string unitOfMeasureCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasureCode);
        return new Quantity(value, unitOfMeasureCode.ToUpperInvariant());
    }

    public Quantity Round() =>
        new(decimal.Round(Value, Scale, MidpointRounding.AwayFromZero), UnitOfMeasureCode);

    public Quantity Add(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Value + other.Value, UnitOfMeasureCode);
    }

    public Quantity Subtract(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Value - other.Value, UnitOfMeasureCode);
    }

    public static Quantity operator +(Quantity left, Quantity right) => left.Add(right);

    public static Quantity operator -(Quantity left, Quantity right) => left.Subtract(right);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return UnitOfMeasureCode;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Value:0.######} {UnitOfMeasureCode}");

    private void EnsureSameUnit(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // Adding 3 metres to 2 kilograms is a bug, not a business rule.
        // Convert through UomConversion first.
        if (!string.Equals(UnitOfMeasureCode, other.UnitOfMeasureCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot combine {UnitOfMeasureCode} with {other.UnitOfMeasureCode}. Convert explicitly first.");
        }
    }
}
