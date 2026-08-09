namespace Erp.Contracts.Masters;

/// <summary>
/// Payload for updating a section, assembly or sub-assembly.
/// <para>
/// The code is absent by design: it is the business key that every drawing,
/// mapping and report refers to, and renaming it silently re-points all of them.
/// The level is absent for the same reason — moving a node between levels is not
/// an edit, it is a different record.
/// </para>
/// </summary>
public sealed record UpdateAssemblyNodeRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// Re-parenting is allowed — a node moved to a different section is a normal
    /// engineering change — but the new parent must still be at the level directly
    /// above, and a node can never become its own ancestor.
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Sent whole. This is a replace, not a patch: a field left out is cleared,
    /// because a form that submits everything it shows and a server that ignores
    /// blanks is how a value nobody can delete comes about.
    /// </summary>
    public AssemblyNodeAttributesDto? Attributes { get; init; }

    /// <summary>
    /// Whether the node may still be used. Editable here rather than through a
    /// separate endpoint: it is a checkbox on the same form, and the legacy screen
    /// treated it the same way.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>Base64 <c>rowversion</c> exactly as received from the detail endpoint.</summary>
    public required string RowVersion { get; init; }
}
