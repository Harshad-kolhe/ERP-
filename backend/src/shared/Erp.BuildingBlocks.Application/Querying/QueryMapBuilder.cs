using System.Linq.Expressions;

namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>
/// Declares the fields a list endpoint exposes.
/// <para>
/// Typical use, as a <c>static readonly</c> on the query handler:
/// </para>
/// <code>
/// private static readonly QueryMap&lt;PartRow&gt; Map = QueryMap&lt;PartRow&gt;.Create()
///     .Field("partNumber", x => x.PartNumber, searchable: true)
///     .Field("description", x => x.Description, searchable: true)
///     .Field("status", x => x.Status)
///     .Field("createdAt", x => x.CreatedAtUtc)
///     .DefaultSort("partNumber")
///     .TieBreaker(x => x.Id)
///     .Build();
/// </code>
/// </summary>
public sealed class QueryMapBuilder<T>
{
    private readonly Dictionary<string, IQueryField<T>> _fields =
        new(StringComparer.OrdinalIgnoreCase);

    private IQueryField<T>? _tieBreaker;
    private string? _defaultSortField;
    private bool _defaultSortDescending;

    /// <summary>
    /// Exposes one field to clients.
    /// </summary>
    /// <param name="name">The name clients use. Use camelCase to match the JSON contract.</param>
    /// <param name="selector">
    /// Selector over the projected row type. Project first, then map: pointing the
    /// map at a DTO rather than an entity keeps domain internals off the wire and
    /// keeps the generated SQL to the columns the grid actually shows.
    /// </param>
    /// <param name="sortable">Whether <c>sort=</c> may name this field. Only enable where an index supports it.</param>
    /// <param name="filterable">Whether <c>filter=</c> may name this field.</param>
    /// <param name="searchable">Whether free-text <c>search=</c> includes this field. Ignored for non-text fields.</param>
    public QueryMapBuilder<T> Field<TProp>(
        string name,
        Expression<Func<T, TProp>> selector,
        bool sortable = true,
        bool filterable = true,
        bool searchable = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(selector);

        if (!_fields.TryAdd(name, new QueryField<T, TProp>(name, selector, sortable, filterable, searchable)))
        {
            throw new InvalidOperationException($"Field '{name}' is declared twice on this query map.");
        }

        return this;
    }

    /// <summary>Ordering used when the client sends no <c>sort=</c>.</summary>
    public QueryMapBuilder<T> DefaultSort(string field, bool descending = false)
    {
        _defaultSortField = field;
        _defaultSortDescending = descending;
        return this;
    }

    /// <summary>
    /// A unique, immutable key appended to every ordering. Required.
    /// Use the primary key; anything non-unique leaves the order under-determined
    /// and paging can repeat or skip rows.
    /// </summary>
    public QueryMapBuilder<T> TieBreaker<TProp>(Expression<Func<T, TProp>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        // Not registered in _fields: it is applied internally and is not part of
        // the client-facing surface.
        _tieBreaker = new QueryField<T, TProp>("__tieBreaker", selector, sortable: true, filterable: false, searchable: false);
        return this;
    }

    public QueryMap<T> Build()
    {
        if (_tieBreaker is null)
        {
            throw new InvalidOperationException(
                $"QueryMap<{typeof(T).Name}> needs a TieBreaker. Without a unique final sort key, paging is not stable.");
        }

        if (string.IsNullOrWhiteSpace(_defaultSortField))
        {
            throw new InvalidOperationException(
                $"QueryMap<{typeof(T).Name}> needs a DefaultSort so results are deterministic when the client sends no sort.");
        }

        if (!_fields.TryGetValue(_defaultSortField, out var defaultField) || !defaultField.Sortable)
        {
            throw new InvalidOperationException(
                $"DefaultSort '{_defaultSortField}' is not a sortable field on QueryMap<{typeof(T).Name}>.");
        }

        return new QueryMap<T>(_fields, _tieBreaker, _defaultSortField, _defaultSortDescending);
    }
}
