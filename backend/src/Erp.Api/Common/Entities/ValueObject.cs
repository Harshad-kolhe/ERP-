namespace Erp.Api.Common.Entities;

/// <summary>
/// Base class for types with no identity, compared by their components.
/// A <c>Gstin</c> or an <c>HsnCode</c> is a value object; making it a type rather
/// than a <see cref="string"/> means it can validate itself once, at construction,
/// instead of being re-checked (or not) at every call site.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The components that define equality, in a stable order.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        return GetType() == other.GetType()
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(GetType());

        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
