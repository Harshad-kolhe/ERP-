namespace Erp.Api.Domain.Parts;

/// <summary>
/// Lifecycle states a part moves through. Transitions are enforced by <see cref="Part"/>.
/// <para>
/// Kept as its own enum rather than reusing <c>MasterStatus</c>, even though the
/// members match today: a part's approval rules are its own, and collapsing the
/// two would mean a change to one silently changing the other's wire contract.
/// </para>
/// </summary>
public enum PartStatus
{
    /// <summary>Being drafted. Editable, and invisible to purchasing and BOM.</summary>
    Draft = 0,

    /// <summary>Submitted for review. Deliberately not editable â€” see <see cref="Part.Update"/>.</summary>
    PendingApproval = 1,

    /// <summary>Approved for use on purchase orders, BOMs and stock transactions.</summary>
    Approved = 2,

    /// <summary>
    /// Sent back by the approver, with a reason. Not a silent return to
    /// <see cref="Draft"/> â€” the legacy flow recorded the rejection but reset the
    /// status, so nothing on screen said the part had been refused.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Approved but paused â€” legacy status <c>10</c>. Not the same as refused, and
    /// not the same as withdrawn, which is <c>IsActive = false</c>.
    /// </summary>
    Hold = 5,

    // No Inactive: that is IsActive. See MasterStatus for why the two are not one
    // field. 3 is left unused so an old persisted value cannot become another state.
}
