using Erp.Contracts.Common;

namespace Erp.Api.Common.Http;

/// <summary>
/// Builds a <see cref="PageRequest"/> from a list endpoint's query parameters.
/// <para>
/// List endpoints declare <c>page</c>, <c>pageSize</c>, <c>sort</c>, <c>search</c>
/// and <c>filter</c> as explicit parameters rather than binding the whole record
/// with <c>[AsParameters]</c>. Two reasons: <see cref="PageRequest"/> lives in
/// <c>Erp.Contracts</c>, which is deliberately dependency-free and therefore cannot
/// carry a <c>BindAsync</c>; and explicit parameters appear individually in the
/// OpenAPI document, so the generated TypeScript client exposes them by name.
/// </para>
/// </summary>
public static class PageRequestBinding
{
    public static PageRequest From(
        int? page,
        int? pageSize,
        string? sort,
        string? search,
        string? filter) =>
        new PageRequest
        {
            Page = page ?? 1,
            PageSize = pageSize ?? PageRequest.DefaultPageSize,
            Sort = sort,
            Search = search,
            Filter = filter,
        }

        // Clamped here as well as in the handler. The ceiling is not negotiable
        // and should not depend on any single caller remembering it.
        .Normalize();
}
