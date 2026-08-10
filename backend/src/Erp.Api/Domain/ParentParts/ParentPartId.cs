namespace Erp.Api.Domain.ParentParts;

/// <summary>
/// Identifier for a <see cref="ParentPart"/>.
/// <para>
/// Distinct from <c>PartId</c> even though a parent part always names one: the two
/// answer different questions â€” "which part" and "which build of that part" â€” and
/// making them the same type is how a query silently reads the wrong table.
/// </para>
/// </summary>
public readonly record struct ParentPartId(Guid Value)
{
    public static ParentPartId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
