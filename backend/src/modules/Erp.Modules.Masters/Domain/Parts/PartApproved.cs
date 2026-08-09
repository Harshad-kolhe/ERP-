using Erp.SharedKernel.Events;

namespace Erp.Modules.Masters.Domain.Parts;

/// <summary>
/// Raised when a part becomes usable on purchase orders, BOMs and stock
/// transactions. Dispatched in-process after the transaction commits.
/// </summary>
internal sealed record PartApproved(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    PartId PartId,
    string PartNumber,
    Guid ApprovedByUserId) : IDomainEvent;
