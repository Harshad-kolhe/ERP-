namespace Erp.Contracts.Masters;

/// <summary>
/// Where a node sits in the machine breakdown: Section › Assembly › Sub-assembly.
/// <para>
/// One enum rather than three record types because the three are the same record
/// with a different depth — the legacy system stored all of them in a single
/// <c>Assembly</c> table discriminated by a <c>Level</c> column holding
/// <c>"S"</c>, <c>"A"</c> or <c>"SA"</c>, and that part of its design was right.
/// What was wrong was everything around it: the parent link was the parent's
/// <em>code</em> as free text with no foreign key, a section pointed at the
/// sentinel <c>"000"</c> instead of null, and the level was a two-character string
/// nothing validated.
/// </para>
/// <para>
/// Serialised as a name, not an ordinal, so a stored value reads
/// <c>SubAssembly</c> rather than <c>2</c>.
/// </para>
/// </summary>
public enum AssemblyLevelDto
{
    /// <summary>Top of the breakdown. Has no parent.</summary>
    Section = 0,

    /// <summary>Belongs to exactly one <see cref="Section"/>.</summary>
    Assembly = 1,

    /// <summary>Belongs to exactly one <see cref="Assembly"/>.</summary>
    SubAssembly = 2,
}
