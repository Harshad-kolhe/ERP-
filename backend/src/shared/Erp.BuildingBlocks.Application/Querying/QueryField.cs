using System.Linq.Expressions;
using System.Reflection;
using Erp.SharedKernel.Results;

namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>One allow-listed field on a <see cref="QueryMap{T}"/>.</summary>
internal interface IQueryField<T>
{
    string Name { get; }

    bool Sortable { get; }

    bool Filterable { get; }

    bool Searchable { get; }

    IOrderedQueryable<T> ApplySort(IQueryable<T> source, bool descending, bool isFirstTerm);

    Result<Expression<Func<T, bool>>> BuildFilter(FilterOperator op, string rawValue);

    Expression<Func<T, bool>>? BuildSearch(string term);
}

/// <summary>
/// Binds a client-facing field name to a strongly-typed selector expression.
/// <para>
/// Because the selector is a real <see cref="Expression"/> rather than a column
/// name in a string, every sort and filter is composed by LINQ and parameterised
/// by EF Core. There is no code path from user input to SQL text.
/// </para>
/// </summary>
internal sealed class QueryField<T, TProp> : IQueryField<T>
{
    private static readonly MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    private static readonly MethodInfo StartsWithMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    private readonly Expression<Func<T, TProp>> _selector;

    public QueryField(
        string name,
        Expression<Func<T, TProp>> selector,
        bool sortable,
        bool filterable,
        bool searchable)
    {
        Name = name;
        _selector = selector;
        Sortable = sortable;
        Filterable = filterable;

        // Only text can participate in free-text search.
        Searchable = searchable && typeof(TProp) == typeof(string);
    }

    public string Name { get; }

    public bool Sortable { get; }

    public bool Filterable { get; }

    public bool Searchable { get; }

    public IOrderedQueryable<T> ApplySort(IQueryable<T> source, bool descending, bool isFirstTerm)
    {
        if (isFirstTerm)
        {
            return descending ? source.OrderByDescending(_selector) : source.OrderBy(_selector);
        }

        var ordered = (IOrderedQueryable<T>)source;
        return descending ? ordered.ThenByDescending(_selector) : ordered.ThenBy(_selector);
    }

    public Result<Expression<Func<T, bool>>> BuildFilter(FilterOperator op, string rawValue)
    {
        var underlying = Nullable.GetUnderlyingType(typeof(TProp)) ?? typeof(TProp);
        var isText = underlying == typeof(string);

        if ((op is FilterOperator.Contains or FilterOperator.StartsWith) && !isText)
        {
            return Result.Failure<Expression<Func<T, bool>>>(QueryErrors.UnsupportedOperator(Name, op));
        }

        var isOrdering = op is FilterOperator.GreaterThan or FilterOperator.GreaterThanOrEqual
            or FilterOperator.LessThan or FilterOperator.LessThanOrEqual;

        if (isOrdering && isText)
        {
            // Ordering comparisons on text depend on collation and rarely mean what
            // the caller expects. Reject rather than produce a surprising result.
            return Result.Failure<Expression<Func<T, bool>>>(QueryErrors.UnsupportedOperator(Name, op));
        }

        if (!QueryValueParser.TryParse(typeof(TProp), rawValue, out var parsed))
        {
            return Result.Failure<Expression<Func<T, bool>>>(QueryErrors.InvalidValue(Name, rawValue));
        }

        Expression constant = Expression.Constant(parsed, underlying);

        if (underlying != typeof(TProp))
        {
            constant = Expression.Convert(constant, typeof(TProp));
        }

        // Explicitly typed: the arms produce BinaryExpression and MethodCallExpression,
        // which have no common natural type.
        Expression? body = op switch
        {
            FilterOperator.Equal => Expression.Equal(_selector.Body, constant),
            FilterOperator.NotEqual => Expression.NotEqual(_selector.Body, constant),
            FilterOperator.GreaterThan => Expression.GreaterThan(_selector.Body, constant),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(_selector.Body, constant),
            FilterOperator.LessThan => Expression.LessThan(_selector.Body, constant),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(_selector.Body, constant),
            FilterOperator.Contains => Expression.Call(_selector.Body, ContainsMethod, constant),
            FilterOperator.StartsWith => Expression.Call(_selector.Body, StartsWithMethod, constant),
            _ => null,
        };

        if (body is null)
        {
            return Result.Failure<Expression<Func<T, bool>>>(QueryErrors.UnsupportedOperator(Name, op));
        }

        return Result.Success(Expression.Lambda<Func<T, bool>>(body, _selector.Parameters[0]));
    }

    public Expression<Func<T, bool>>? BuildSearch(string term)
    {
        if (!Searchable)
        {
            return null;
        }

        var body = Expression.Call(_selector.Body, ContainsMethod, Expression.Constant(term, typeof(string)));
        return Expression.Lambda<Func<T, bool>>(body, _selector.Parameters[0]);
    }
}
