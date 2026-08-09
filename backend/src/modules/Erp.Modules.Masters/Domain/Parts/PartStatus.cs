namespace Erp.Modules.Masters.Domain.Parts;

/// <summary>Lifecycle states a part moves through. Transitions are enforced by <see cref="Part"/>.</summary>
internal enum PartStatus
{
    /// <summary>Being drafted. Editable, and invisible to purchasing and BOM.</summary>
    Draft = 0,

    /// <summary>Submitted for review. Deliberately not editable — see <see cref="Part.Update"/>.</summary>
    PendingApproval = 1,

    /// <summary>Approved for use on purchase orders, BOMs and stock transactions.</summary>
    Approved = 2,

    /// <summary>Withdrawn from new use. History referencing it stays intact.</summary>
    Inactive = 3,
}
