namespace Erp.Contracts.Masters;

/// <summary>
/// Lifecycle of a part master record, serialised as a string.
/// <para>
/// The legacy schema encoded status as bare literals — <c>"01"</c> appeared 277
/// times, <c>"02"</c> 178 times — with a single enum in the entire solution. A
/// named type means an unhandled state is a compiler error and the wire format
/// is self-describing.
/// </para>
/// </summary>
public enum PartStatusDto
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Inactive = 3,
}
