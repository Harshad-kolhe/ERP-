namespace Erp.Modules.Masters.Domain.Assemblies;

/// <summary>
/// Identifier for an <see cref="AssemblyNode"/>.
/// <para>
/// A distinct type rather than a bare <see cref="Guid"/>, so passing a part id
/// where an assembly id belongs is a compile error instead of a query that
/// silently returns nothing.
/// </para>
/// </summary>
internal readonly record struct AssemblyNodeId(Guid Value)
{
    /// <summary>
    /// Allocates a new identifier. Version 7 UUIDs are time-ordered, so inserts
    /// land at the end of the clustered index instead of scattering across it.
    /// </summary>
    public static AssemblyNodeId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
