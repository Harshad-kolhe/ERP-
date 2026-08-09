namespace Erp.Contracts.Common;

/// <summary>
/// Keyset-paged results, for feeds with no useful total and no stable offset —
/// the stock ledger and the parts ledger above all.
/// <para>
/// Offset paging degrades badly on those tables: <c>OFFSET 500000 ROWS</c> makes
/// SQL Server walk half a million rows, and a row inserted mid-scroll shifts every
/// subsequent page. A cursor encodes the last key seen, so page N+1 costs the same
/// as page 1 and never repeats or skips a row.
/// </para>
/// </summary>
public sealed record CursorPage<T>
{
    public CursorPage(IReadOnlyList<T> items, string? nextCursor)
    {
        Items = items;
        NextCursor = nextCursor;
    }

    public IReadOnlyList<T> Items { get; init; }

    /// <summary>Opaque token to pass back for the following page. <c>null</c> at the end of the feed.</summary>
    public string? NextCursor { get; init; }

    public bool HasMore => NextCursor is not null;

    public static CursorPage<T> Empty() => new([], null);
}
