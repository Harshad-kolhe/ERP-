namespace Erp.Api.Domain.Common;

/// <summary>
/// Where a master record sits in its approval lifecycle.
/// <para>
/// One small set, shared by every master, because approval means the same thing
/// everywhere. It is deliberately <em>not</em> a general-purpose status: a goods
/// receipt or a job card gets its own enum in its own module. The legacy system
/// had a single code space where <c>"02"</c> meant "Approved" on a part, "Pending
/// Store Loc" on a GRN and "QC Partial" on the quality flow â€” which is how a
/// codebase ends up with 262 bare occurrences of <c>"01"</c> that mean different
/// things.
/// </para>
/// <para>
/// Stored as text through a value converter, so the column reads <c>Approved</c>
/// rather than a code nobody can decode without the lookup table.
/// </para>
/// </summary>
public enum MasterStatus
{
    /// <summary>Being drafted. Editable, and not yet usable by downstream modules.</summary>
    Draft = 0,

    /// <summary>Submitted for review. Not editable â€” the approver must see what they approve.</summary>
    PendingApproval = 1,

    /// <summary>Approved for use.</summary>
    Approved = 2,

    /// <summary>
    /// Sent back by the approver.
    /// <para>
    /// A state of its own rather than a silent return to <see cref="Draft"/>. The
    /// legacy tables carried <c>RejectedBy</c>, <c>RejectedOn</c> and
    /// <c>RejectedReason</c>, so the rejection was recorded â€” but the status went
    /// back to pending, so nothing on screen said the record had been refused, and
    /// the reason was invisible unless somebody queried the table.
    /// </para>
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Approved, but temporarily not to be used â€” legacy status <c>10</c>.
    /// <para>
    /// Distinct from <see cref="Rejected"/>: a held record was accepted and is
    /// paused, not refused. Distinct from <c>IsActive = false</c> too, which is
    /// permanent withdrawal rather than a pause.
    /// </para>
    /// </summary>
    Hold = 5,

    // No Inactive. Whether a record may be used on new documents is IsActive, and
    // saying it twice invites the two to disagree â€” a record cannot be Approved and
    // Inactive at once in one field, which is exactly why the boolean exists.
    // 3 is left unused rather than reassigned, so an old persisted value can never
    // silently become a different state.
}
