namespace Erp.Modules.Masters.Domain.Assemblies;

/// <summary>
/// Depth of a node in the machine breakdown.
/// <para>
/// The legacy column held <c>"S"</c>, <c>"A"</c> or <c>"SA"</c> as free text, and
/// the string <c>"SA"</c> starts with <c>"S"</c> — which is why the legacy code
/// that generated codes with <c>AssemblyCode.StartsWith("S")</c> counted every
/// sub-assembly as a section. Stored here as the enum name through a value
/// converter, so the column reads <c>SubAssembly</c> and no prefix test can
/// confuse the two.
/// </para>
/// </summary>
internal enum AssemblyLevel
{
    Section = 0,
    Assembly = 1,
    SubAssembly = 2,
}

/// <summary>
/// The one place that says which level may sit under which.
/// <para>
/// Written as data rather than as <c>if</c> statements spread through three save
/// methods, because that is exactly how the legacy rules diverged: sections
/// checked nothing, assemblies checked that the parent was a section, and
/// sub-assemblies accepted a section <em>or</em> an assembly and then applied an
/// extra rule about sibling sub-assemblies that no other level had. Changing the
/// shape of the tree is a change to this table and nowhere else.
/// </para>
/// </summary>
internal static class AssemblyLevels
{
    /// <summary>
    /// The level a node's parent must be, or <c>null</c> when the level has no
    /// parent at all.
    /// </summary>
    public static AssemblyLevel? ParentOf(AssemblyLevel level) => level switch
    {
        AssemblyLevel.Section => null,
        AssemblyLevel.Assembly => AssemblyLevel.Section,
        AssemblyLevel.SubAssembly => AssemblyLevel.Assembly,
        _ => null,
    };

    /// <summary>Whether a node at this level must name a parent.</summary>
    public static bool RequiresParent(AssemblyLevel level) => ParentOf(level) is not null;

    /// <summary>Human-readable name used in error messages, so "SubAssembly" reads as "sub-assembly".</summary>
    public static string Describe(AssemblyLevel level) => level switch
    {
        AssemblyLevel.Section => "section",
        AssemblyLevel.Assembly => "assembly",
        AssemblyLevel.SubAssembly => "sub-assembly",
        _ => level.ToString(),
    };
}
