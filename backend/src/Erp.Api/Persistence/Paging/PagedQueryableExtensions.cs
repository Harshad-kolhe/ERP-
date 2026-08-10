using Erp.Api.Common.Paging;
using Erp.Contracts.Common;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Persistence.Paging;

/// <summary>
/// The single sanctioned way to turn a query into a page of results.
/// </summary>
public static class PagedQueryableExtensions
{
    /// <summary>
    /// Applies a <see cref="QueryMap{T}"/>, counts the matching rows, and returns
    /// exactly one page.
    /// <para>
    /// Two round trips by design â€” a <c>COUNT</c> and a <c>SELECT TOP</c>. The
    /// alternative (window function in the same statement) is one round trip but
    /// forces SQL Server to materialise the full result set to compute the count,
    /// which is precisely the cost this method exists to avoid.
    /// </para>
    /// </summary>
    public static async Task<Result<PagedResult<T>>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        QueryMap<T> map,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(request);

        var normalized = request.Normalize();
        var shaped = map.Apply(source, normalized);

        if (shaped.IsFailure)
        {
            return Result.Failure<PagedResult<T>>(shaped.Error);
        }

        var query = shaped.Value;

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .Skip(normalized.Skip)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<T>(items, normalized.Page, normalized.PageSize, totalCount));
    }
}
