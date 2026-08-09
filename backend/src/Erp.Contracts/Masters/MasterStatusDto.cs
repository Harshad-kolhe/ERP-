namespace Erp.Contracts.Masters;

/// <summary>
/// Approval lifecycle of the master records ported from the legacy system —
/// suppliers, customers, employees and roles — serialised as a string.
/// <para>
/// Kept distinct from <see cref="PartStatusDto"/> even though the members match
/// today: parts have their own approval rules, and collapsing the two would mean
/// a later change to one silently changing the wire contract of the other.
/// </para>
/// </summary>
public enum MasterStatusDto
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Inactive = 3,
}
