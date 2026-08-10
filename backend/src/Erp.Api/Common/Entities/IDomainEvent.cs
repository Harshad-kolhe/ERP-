namespace Erp.Api.Common.Entities;

/// <summary>
/// Something that happened inside one aggregate, in the past tense
/// (<c>PartApproved</c>, <c>GoodsReceiptPosted</c>).
/// <para>
/// Dispatched in-process after the transaction commits. For anything that must
/// cross a module boundary, publish an integration event through the outbox
/// instead â€” a domain event is an implementation detail of its module.
/// </para>
/// </summary>
public interface IDomainEvent
{
    /// <summary>Correlates the event with the request that produced it.</summary>
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}
