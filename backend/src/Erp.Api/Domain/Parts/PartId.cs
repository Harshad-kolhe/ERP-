namespace Erp.Api.Domain.Parts;

/// <summary>
/// Identifier for a <see cref="Part"/>.
/// <para>
/// A distinct type rather than a bare <see cref="Guid"/>, so passing a supplier id
/// where a part id belongs is a compile error instead of a query that silently
/// returns nothing.
/// </para>
/// </summary>
public readonly record struct PartId(Guid Value)
{
    /// <summary>
    /// Allocates a new identifier.
    /// <para>
    /// Version 7 UUIDs are time-ordered, so inserts land at the end of the clustered
    /// index instead of scattering across it. Random <see cref="Guid.NewGuid"/> keys
    /// fragment the index and turn every insert into a page split â€” which matters on
    /// tables that take millions of rows a year.
    /// </para>
    /// </summary>
    public static PartId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
