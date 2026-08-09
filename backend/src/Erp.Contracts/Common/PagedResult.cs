namespace Erp.Contracts.Common;

/// <summary>
/// One page of results plus the totals a grid needs to render its pager.
/// <para>
/// Every list endpoint must return this type (or <see cref="CursorPage{T}"/>).
/// <c>EndpointConventionTests</c> fails the build if one returns a bare
/// collection, which makes "accidentally return the whole table" unexpressible
/// rather than merely discouraged.
/// </para>
/// </summary>
public sealed record PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    /// <summary>Total matching rows across all pages, for the pager display.</summary>
    public long TotalCount { get; init; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)((TotalCount + PageSize - 1) / PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Empty(int pageSize) => new([], 1, pageSize, 0);
}
