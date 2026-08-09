using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Erp.ArchitectureTests.Documentation;

/// <summary>
/// Renders one database schema as a Mermaid <c>erDiagram</c>, from the EF Core model
/// that the application actually built.
/// <para>
/// A hand-drawn ERD documents what someone believed the schema was on the day they
/// drew it. This one cannot: it is a projection of the same metadata EF uses to emit
/// SQL, so a diagram that disagrees with the database is not expressible.
/// </para>
/// <para>
/// The renderer is pure — no I/O, no host — so its output can be asserted directly,
/// and it is deterministic, so regenerating without a model change produces a
/// byte-identical file and leaves the working tree clean.
/// </para>
/// </summary>
internal static class MermaidErd
{
    /// <summary>
    /// Marks generated files. Anyone opening one sees, on line 1, that editing it is
    /// pointless — the next test run overwrites it.
    /// </summary>
    public const string GeneratedBanner =
        "<!-- Generated from the EF Core model by Erp.ArchitectureTests. Do not edit. -->";

    public static string Render(string schema, IReadOnlyCollection<IEntityType> entityTypes)
    {
        var tables = TablesIn(entityTypes);
        var builder = new StringBuilder();

        builder
            .Append(GeneratedBanner).Append('\n')
            .Append('\n')
            .Append(CultureInfo.InvariantCulture, $"# Database schema `{schema}`").Append('\n')
            .Append('\n')
            .Append(CultureInfo.InvariantCulture, $"{tables.Count} {(tables.Count == 1 ? "table" : "tables")}. Regenerate with:").Append('\n')
            .Append('\n')
            .Append("```\n")
            .Append("dotnet test backend/tests/Erp.ArchitectureTests --filter FullyQualifiedName~SchemaDiagram\n")
            .Append("```\n")
            .Append('\n');

        AppendDiagram(builder, tables);
        AppendUnenforcedReferences(builder, tables);

        return builder.ToString();
    }

    private static void AppendDiagram(StringBuilder builder, IReadOnlyList<Table> tables)
    {
        builder.Append("```mermaid\nerDiagram\n");

        foreach (var table in tables)
        {
            builder.Append(CultureInfo.InvariantCulture, $"    {table.Name} {{\n");

            foreach (var column in table.Columns)
            {
                builder.Append(CultureInfo.InvariantCulture, $"        {column.StoreType} {column.Name}");

                if (column.Keys.Count != 0)
                {
                    builder.Append(' ').Append(string.Join(",", column.Keys));
                }

                if (column.IsNullable)
                {
                    builder.Append(" \"nullable\"");
                }

                builder.Append('\n');
            }

            builder.Append("    }\n");
        }

        var relationships = tables.SelectMany(table => table.Relationships).Distinct().Order(StringComparer.Ordinal);

        foreach (var relationship in relationships)
        {
            builder.Append(CultureInfo.InvariantCulture, $"    {relationship}\n");
        }

        builder.Append("```\n");
    }

    /// <summary>
    /// Lists columns that name a relationship the database does not enforce.
    /// <para>
    /// This exists because the model currently declares almost no foreign keys, so a
    /// diagram alone would render disconnected boxes and quietly imply the links are
    /// absent by design. Rows leave this table as real constraints land — it shrinks
    /// as the schema improves, rather than needing to be maintained.
    /// </para>
    /// </summary>
    private static void AppendUnenforcedReferences(StringBuilder builder, IReadOnlyList<Table> tables)
    {
        var candidates = tables
            .SelectMany(table => table.Columns
                .Where(column => column.LooksLikeAReference)
                .Select(column => (Table: table.Name, column.Name, column.StoreType)))
            .ToList();

        builder
            .Append('\n')
            .Append("## Columns that name a relationship the database does not enforce\n")
            .Append('\n');

        if (candidates.Count == 0)
        {
            builder.Append("None — every reference in this schema is a real foreign key.\n");
            return;
        }

        builder
            .Append(
                "Found by name (`*Id`, not a primary key, not covered by any foreign key), so the list\n"
                + "is a prompt to look rather than a defect list. Some entries are deliberate: audit\n"
                + "columns and anything pointing into another module's schema cannot be constrained,\n"
                + "because each module owns a separate `DbContext`. See `db/erd/README.md`.\n")
            .Append('\n')
            .Append("| Table | Column | Type |\n")
            .Append("|---|---|---|\n");

        foreach (var (table, column, storeType) in candidates)
        {
            builder.Append(CultureInfo.InvariantCulture, $"| {table} | {column} | `{storeType}` |\n");
        }
    }

    /// <summary>
    /// Projects entity types onto the physical tables they map to. Grouping by table
    /// rather than by CLR type keeps the diagram honest under inheritance and owned
    /// types, where several entity types share one table.
    /// </summary>
    private static List<Table> TablesIn(IReadOnlyCollection<IEntityType> entityTypes) =>
        [.. entityTypes
            .Where(entityType => entityType.GetTableName() is not null)
            .GroupBy(entityType => entityType.GetTableName()!, StringComparer.Ordinal)
            .Select(group => new Table(
                Identifier(group.Key),
                ColumnsIn(group),
                [.. group.SelectMany(Relationships)]))
            .OrderBy(table => table.Name, StringComparer.Ordinal)];

    private static List<Column> ColumnsIn(IEnumerable<IEntityType> entityTypes)
    {
        var columns = new Dictionary<string, Column>(StringComparer.Ordinal);
        var keyOrder = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var entityType in entityTypes)
        {
            var primaryKey = (entityType.FindPrimaryKey()?.Properties ?? [])
                .Select((property, index) => (property, index))
                .ToDictionary(entry => entry.property, entry => entry.index);

            var foreignKeyProperties = entityType.GetForeignKeys()
                .SelectMany(foreignKey => foreignKey.Properties)
                .ToHashSet();

            foreach (var property in entityType.GetProperties())
            {
                var name = property.GetColumnName();

                if (columns.ContainsKey(name))
                {
                    continue;
                }

                var keyIndex = primaryKey.TryGetValue(property, out var index) ? index : -1;
                var isForeignKey = foreignKeyProperties.Contains(property);

                List<string> keys = [];

                if (keyIndex >= 0)
                {
                    keys.Add("PK");
                    keyOrder[name] = keyIndex;
                }

                if (isForeignKey)
                {
                    keys.Add("FK");
                }

                columns[name] = new Column(
                    name,
                    StoreType(property),
                    keys,
                    // A primary key column is never reported as nullable: EF models
                    // an identity key as non-nullable regardless of CLR nullability.
                    property.IsNullable && keyIndex < 0,
                    LooksLikeAReference: keyIndex < 0
                        && !isForeignKey
                        && name.EndsWith("Id", StringComparison.Ordinal)
                        && CanBeAKey(property));
            }
        }

        // Primary key first, in key order, then everything else alphabetically. Stable
        // across machines and runs, and it still reads like a table definition.
        return [.. columns.Values
            .OrderBy(column => keyOrder.TryGetValue(column.Name, out var index) ? index : int.MaxValue)
            .ThenBy(column => column.Name, StringComparer.Ordinal)];
    }

    private static IEnumerable<string> Relationships(IEntityType entityType) =>
        entityType.GetForeignKeys()
            .Where(foreignKey => foreignKey.PrincipalEntityType.GetTableName() is not null
                && foreignKey.DeclaringEntityType.GetTableName() is not null)
            .Select(foreignKey =>
            {
                var principal = Identifier(foreignKey.PrincipalEntityType.GetTableName()!);
                var dependent = Identifier(foreignKey.DeclaringEntityType.GetTableName()!);

                // The marker nearest each entity states that entity's cardinality: an
                // optional foreign key means the dependent row may have no principal.
                var principalSide = foreignKey.IsRequired ? "||" : "|o";
                var dependentSide = foreignKey.IsUnique ? "||" : "o{";
                var label = string.Join(", ", foreignKey.Properties.Select(property => property.GetColumnName()));

                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"{principal} {principalSide}--{dependentSide} {dependent} : \"{label}\"");
            });

    /// <summary>
    /// Narrows the <c>*Id</c> name test to types this system actually uses for keys.
    /// Without it the list fills with codes that merely end in "Id" — <c>TaxId</c>,
    /// <c>ProgramId</c> — and a list with obvious noise in it stops being read.
    /// </summary>
    private static bool CanBeAKey(IProperty property)
    {
        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

        return clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(Guid);
    }

    /// <summary>
    /// Mermaid attribute types are single tokens: a literal <c>decimal(18,4)</c> or
    /// <c>nvarchar(50)</c> fails to parse, and every decimal in this system is
    /// <c>(18,4)</c> by convention, so this is load-bearing rather than defensive.
    /// </summary>
    private static string StoreType(IProperty property)
    {
        var storeType = property.GetColumnType() ?? property.ClrType.Name;

        return storeType
            .Replace("(", "_", StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace(",", "_", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>Mermaid entity names admit only word characters.</summary>
    private static string Identifier(string name) =>
        string.Concat(name.Select(character => char.IsLetterOrDigit(character) ? character : '_'));

    private sealed record Column(
        string Name,
        string StoreType,
        IReadOnlyList<string> Keys,
        bool IsNullable,
        bool LooksLikeAReference);

    private sealed record Table(
        string Name,
        IReadOnlyList<Column> Columns,
        IReadOnlyList<string> Relationships);
}
