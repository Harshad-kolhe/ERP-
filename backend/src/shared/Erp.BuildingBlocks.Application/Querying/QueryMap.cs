using System.Linq.Expressions;
using Erp.Contracts.Common;
using Erp.SharedKernel.Results;

namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>
/// The allow-list of fields a list endpoint permits clients to sort and filter on.
/// <para>
/// This type is the answer to two of the worst properties of the system it
/// replaces at once. Dynamic filtering is expressed as LINQ over pre-declared
/// selector expressions, so there is no path from user input to SQL text (the
/// legacy app had 158 interpolated <c>EXEC</c> sites). And because a field must be
/// declared here to be sortable, every sortable column is a known, finite set that
/// can be index-covered — instead of the database being asked to sort on arbitrary
/// columns at arbitrary scale.
/// </para>
/// <para>
/// A stable tie-breaker is mandatory. Without one, SQL Server may return a row on
/// two different pages of the same result set, which is the kind of defect users
/// report as "the grid is randomly duplicating rows" and nobody can reproduce.
/// </para>
/// </summary>
public sealed class QueryMap<T>
{
    private readonly IReadOnlyDictionary<string, IQueryField<T>> _fields;
    private readonly IReadOnlyList<IQueryField<T>> _searchable;
    private readonly IQueryField<T> _tieBreaker;
    private readonly string _defaultSortField;
    private readonly bool _defaultSortDescending;

    internal QueryMap(
        IReadOnlyDictionary<string, IQueryField<T>> fields,
        IQueryField<T> tieBreaker,
        string defaultSortField,
        bool defaultSortDescending)
    {
        _fields = fields;
        _tieBreaker = tieBreaker;
        _defaultSortField = defaultSortField;
        _defaultSortDescending = defaultSortDescending;
        _searchable = [.. fields.Values.Where(f => f.Searchable)];
    }

    public static QueryMapBuilder<T> Create() => new();

    /// <summary>Field names clients may sort on. Surfaced in OpenAPI so the contract is discoverable.</summary>
    public IReadOnlyCollection<string> SortableFields =>
        [.. _fields.Values.Where(f => f.Sortable).Select(f => f.Name)];

    /// <summary>Field names clients may filter on.</summary>
    public IReadOnlyCollection<string> FilterableFields =>
        [.. _fields.Values.Where(f => f.Filterable).Select(f => f.Name)];

    /// <summary>
    /// Applies filtering, free-text search and ordering. Does not apply paging —
    /// the caller needs the unpaged query to count total rows first.
    /// </summary>
    public Result<IQueryable<T>> Apply(IQueryable<T> source, PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);

        var filtered = source;

        foreach (var term in FilterTerm.Parse(request.Filter))
        {
            if (!_fields.TryGetValue(term.Field, out var field) || !field.Filterable)
            {
                return Result.Failure<IQueryable<T>>(QueryErrors.UnknownFilterField(term.Field));
            }

            var predicate = field.BuildFilter(term.Operator, term.Value);

            if (predicate.IsFailure)
            {
                return Result.Failure<IQueryable<T>>(predicate.Error);
            }

            filtered = filtered.Where(predicate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search) && _searchable.Count > 0)
        {
            var term = request.Search.Trim();
            Expression<Func<T, bool>>? combined = null;

            foreach (var field in _searchable)
            {
                var clause = field.BuildSearch(term);

                if (clause is null)
                {
                    continue;
                }

                combined = combined is null ? clause : PredicateCombiner.Or(combined, clause);
            }

            if (combined is not null)
            {
                filtered = filtered.Where(combined);
            }
        }

        var terms = SortTerm.Parse(request.Sort);

        if (terms.Count == 0)
        {
            terms = [new SortTerm(_defaultSortField, _defaultSortDescending)];
        }

        var sorted = filtered;
        var isFirstTerm = true;

        foreach (var term in terms)
        {
            if (!_fields.TryGetValue(term.Field, out var field) || !field.Sortable)
            {
                return Result.Failure<IQueryable<T>>(QueryErrors.UnknownSortField(term.Field));
            }

            sorted = field.ApplySort(sorted, term.Descending, isFirstTerm);
            isFirstTerm = false;
        }

        // Always last, always ascending: guarantees a total order so paging is stable.
        sorted = _tieBreaker.ApplySort(sorted, descending: false, isFirstTerm: isFirstTerm);

        return Result.Success(sorted);
    }
}
