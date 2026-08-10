namespace Erp.Api.Common.Paging;

/// <summary>One parsed term from a <c>filter=</c> query string.</summary>
/// <param name="Field">Client-facing field name, resolved against the endpoint's <see cref="QueryMap{T}"/>.</param>
/// <param name="Operator">One of the closed <see cref="FilterOperator"/> set.</param>
/// <param name="Value">Raw text, converted to the field's CLR type before use.</param>
public readonly record struct FilterTerm(string Field, FilterOperator Operator, string Value)
{
    /// <summary>
    /// Parses <c>status:eq:Approved;createdAt:gte:2026-01-01</c>.
    /// A term naming an unknown operator is dropped here; a term naming an unknown
    /// <em>field</em> is reported as an error by <see cref="QueryMap{T}"/>, because
    /// silently ignoring it would show the user unfiltered data they believe is filtered.
    /// </summary>
    public static IReadOnlyList<FilterTerm> Parse(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return [];
        }

        var terms = new List<FilterTerm>();

        foreach (var raw in filter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Split into exactly three so the value may itself contain colons
            // (timestamps, for instance).
            var parts = raw.Split(':', 3, StringSplitOptions.TrimEntries);

            if (parts.Length < 3 || parts[0].Length == 0)
            {
                continue;
            }

            if (!TryParseOperator(parts[1], out var op))
            {
                continue;
            }

            terms.Add(new FilterTerm(parts[0], op, parts[2]));
        }

        return terms;
    }

    private static bool TryParseOperator(string token, out FilterOperator op)
    {
        switch (token.ToLowerInvariant())
        {
            case "eq": op = FilterOperator.Equal; return true;
            case "neq": op = FilterOperator.NotEqual; return true;
            case "gt": op = FilterOperator.GreaterThan; return true;
            case "gte": op = FilterOperator.GreaterThanOrEqual; return true;
            case "lt": op = FilterOperator.LessThan; return true;
            case "lte": op = FilterOperator.LessThanOrEqual; return true;
            case "contains": op = FilterOperator.Contains; return true;
            case "startswith": op = FilterOperator.StartsWith; return true;
            default: op = default; return false;
        }
    }
}
