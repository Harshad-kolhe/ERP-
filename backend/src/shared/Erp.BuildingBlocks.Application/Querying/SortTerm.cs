namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>One parsed term from a <c>sort=</c> query string.</summary>
/// <param name="Field">Client-facing field name, resolved against the endpoint's <see cref="QueryMap{T}"/>.</param>
/// <param name="Descending">True for <c>:desc</c>.</param>
public readonly record struct SortTerm(string Field, bool Descending)
{
    /// <summary>
    /// Parses <c>partNumber:asc,createdAt:desc</c>. Malformed terms are skipped
    /// rather than throwing — the field allow-list is what provides safety, and a
    /// stray comma should not fail a user's grid.
    /// </summary>
    public static IReadOnlyList<SortTerm> Parse(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return [];
        }

        var terms = new List<SortTerm>();

        foreach (var raw in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            var field = parts[0];

            if (field.Length == 0)
            {
                continue;
            }

            var descending = parts.Length > 1
                && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            terms.Add(new SortTerm(field, descending));
        }

        return terms;
    }
}
