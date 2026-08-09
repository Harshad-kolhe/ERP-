namespace Erp.Contracts.Common;

/// <summary>
/// The paging, sorting and filtering a client may ask for on a list endpoint.
/// <para>
/// <see cref="PageSize"/> is clamped to <see cref="MaxPageSize"/> by
/// <see cref="Normalize"/>, which every list handler calls. That clamp is the
/// reason a client cannot ask for the whole table: in the system this replaces,
/// roughly 149 of 180 grids fetched every row and paged in the browser, and no
/// server-side limit existed to prevent it.
/// </para>
/// </summary>
public sealed record PageRequest
{
    /// <summary>Hard ceiling on rows per request. Exceeding it is silently clamped, never honoured.</summary>
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Comma-separated sort terms, e.g. <c>partNumber:asc,createdAt:desc</c>.
    /// Each field is resolved against the endpoint's <c>QueryMap</c>; anything
    /// not on that allow-list is rejected rather than concatenated into SQL.
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>Free-text term applied to the endpoint's designated searchable fields.</summary>
    public string? Search { get; init; }

    /// <summary>
    /// Column filters as <c>field:operator:value</c> terms separated by <c>;</c>,
    /// e.g. <c>status:eq:Approved;createdAt:gte:2026-01-01</c>. Resolved through
    /// the same allow-list as <see cref="Sort"/>.
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Clamps the request into a range the server is willing to serve.
    /// Callers must use the returned value, not the original.
    /// </summary>
    public PageRequest Normalize() => this with
    {
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize < 1 ? DefaultPageSize : (PageSize > MaxPageSize ? MaxPageSize : PageSize),
    };

    public int Skip => (Page - 1) * PageSize;
}
