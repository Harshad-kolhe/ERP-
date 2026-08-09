namespace Erp.Contracts.Masters;

/// <summary>
/// Payload for creating a section, assembly or sub-assembly.
/// <para>
/// The level is <em>not</em> on this payload. It comes from the route the caller
/// posted to — <c>/masters/sections</c>, <c>/masters/assemblies</c>,
/// <c>/masters/sub-assemblies</c> — because each route carries its own permission,
/// and a level in the body would let someone holding only
/// <c>masters.section.create</c> create an assembly by changing one JSON field.
/// </para>
/// </summary>
public sealed record CreateAssemblyNodeRequest
{
    /// <summary>
    /// The business key, entered by the user.
    /// <para>
    /// The legacy screen generated it — <c>"S" + (max(existing) + 1)</c>, computed
    /// by reading every row into memory, parsing the numeric tail and taking the
    /// maximum. Two people saving at the same moment got the same code, and the
    /// scheme could not be changed without editing four copies of that block.
    /// Codes are entered here until the shared number-series allocator lands, at
    /// which point this field becomes optional and the server fills it in.
    /// </para>
    /// </summary>
    public required string Code { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// The node above this one.
    /// <para>
    /// Required for an assembly (its section) and for a sub-assembly (its
    /// assembly); rejected for a section, which is the top of the breakdown. The
    /// server checks both that the parent exists and that it is at the right level
    /// — the legacy screen checked only the first, so a sub-assembly could be filed
    /// under another sub-assembly.
    /// </para>
    /// </summary>
    public Guid? ParentId { get; init; }

    public AssemblyNodeAttributesDto? Attributes { get; init; }
}
