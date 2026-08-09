using Erp.Contracts.Common;

namespace Erp.ArchitectureTests;

/// <summary>
/// Keeps the OpenAPI document unambiguous.
/// <para>
/// A schema in the document is keyed by a type's <em>short</em> name, not its
/// namespace. Two contract types called the same thing therefore collapse into one
/// schema, and the document silently describes both endpoints with whichever type
/// happened to be written last.
/// </para>
/// <para>
/// This is not hypothetical. <c>Erp.Contracts.Masters.RoleListItemDto</c> and
/// <c>Erp.Contracts.Security.RoleListItemDto</c> both existed, so
/// <c>GET /api/v1/masters/roles</c> advertised the Security shape — a different
/// primary key type and not one field in common. Every generated client believed
/// it. Nothing failed: the contract drift gate compares the document against
/// itself, so a document that is internally consistent and wrong passes.
/// </para>
/// </summary>
public sealed class ContractSchemaTests
{
    [Fact]
    public void No_two_contract_types_share_a_short_name()
    {
        var collisions = ContractTypes()
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} — {string.Join(" and ", group.Select(type => type.FullName))}")
            .Order(StringComparer.Ordinal)
            .ToList();

        collisions.ShouldBeEmpty(
            "two contract types share a short name, so they collapse into one OpenAPI schema "
            + "and one of the endpoints is described by the wrong shape. Rename one:\n"
            + string.Join('\n', collisions));
    }

    /// <summary>
    /// Guards the guard: if the type scan broke, the assertion above would pass over
    /// an empty set and report success forever.
    /// </summary>
    [Fact]
    public void Contract_scan_actually_finds_types()
    {
        ContractTypes().Count.ShouldBeGreaterThan(20, "the contract type scan found almost nothing.");
    }

    private static List<Type> ContractTypes() =>
        [.. typeof(PagedResult<>).Assembly
            .GetExportedTypes()
            // Nested types are namespaced by their declaring type in the document, so
            // they cannot collide the way top-level ones do.
            .Where(type => !type.IsNested)];
}
