namespace Erp.Modules.Masters.Domain.Common;

/// <summary>
/// Approval lifecycle shared by the master records ported from the legacy system.
/// <para>
/// The legacy tables carried this as a nullable <c>status</c> string holding the
/// literals "01".."10", which is why the old codebase contains 277 bare
/// occurrences of <c>"01"</c>. Stored here as text through a value converter, so
/// the column reads <c>Approved</c> rather than a code nobody can decode without
/// the lookup table.
/// </para>
/// </summary>
internal enum MasterStatus
{
    /// <summary>Being drafted. Editable, and not yet usable by downstream modules.</summary>
    Draft = 0,

    /// <summary>Submitted for review.</summary>
    PendingApproval = 1,

    /// <summary>Approved for use.</summary>
    Approved = 2,

    /// <summary>Withdrawn from new use. History referencing it stays intact.</summary>
    Inactive = 3,
}
