using Erp.SharedKernel.Results;

namespace Erp.Modules.Masters.Domain.Parts;

/// <summary>
/// Every way a part operation can fail, named once.
/// <para>
/// Declaring them together means the API's error contract is readable in a single
/// file, and the codes are stable enough for the web app to branch on.
/// </para>
/// </summary>
internal static class PartErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "part.not_found",
        $"No part with id '{id}' exists in this business unit.");

    public static Error DuplicatePartNumber(string partNumber) => Error.Conflict(
        "part.number.duplicate",
        $"Part number '{partNumber}' is already in use.");

    public static Error NotEditableWhilePendingApproval => Error.Conflict(
        "part.not_editable_pending_approval",
        "A part cannot be edited while it is awaiting approval. Withdraw it first.");

    public static Error CannotSubmitFromStatus(PartStatus status) => Error.Conflict(
        "part.cannot_submit",
        $"Only a draft part can be submitted for approval; this one is {status}.");

    public static Error CannotApproveFromStatus(PartStatus status) => Error.Conflict(
        "part.cannot_approve",
        $"Only a part awaiting approval can be approved; this one is {status}.");

    public static Error ApproverCannotBeAuthor => Error.Conflict(
        "part.approver_is_author",
        "A part cannot be approved by the person who created it.");

    public static Error StaleRowVersion => Error.Conflict(
        "part.stale_row_version",
        "This part was changed by someone else since you loaded it. Reload and try again.");
}
