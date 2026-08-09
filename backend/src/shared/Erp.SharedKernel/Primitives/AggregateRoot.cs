using Erp.SharedKernel.Events;

namespace Erp.SharedKernel.Primitives;

/// <summary>
/// The entry point to a consistency boundary. Only aggregate roots are loaded,
/// saved and referenced across module boundaries; everything else is reached
/// through one.
/// <para>
/// Domain events raised here are collected and dispatched by the persistence
/// layer after <c>SaveChangesAsync</c> succeeds, so a handler can never observe
/// an event for a transaction that later rolled back.
/// </para>
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Parameterless constructor for the persistence provider only.</summary>
    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
