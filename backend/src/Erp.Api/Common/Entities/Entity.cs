namespace Erp.Api.Common.Entities;

/// <summary>
/// Base class for anything with a persistent identity. Equality is identity,
/// never structural.
/// </summary>
/// <typeparam name="TId">The identifier type. Prefer a strongly-typed id over a bare <see cref="int"/>.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>Parameterless constructor for the persistence provider only.</summary>
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Different concrete types are never equal, even with matching ids â€”
        // a PartId of 1 is not a SupplierId of 1.
        return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
