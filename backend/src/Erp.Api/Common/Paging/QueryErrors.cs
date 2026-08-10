using Erp.Api.Common.Results;

namespace Erp.Api.Common.Paging;

/// <summary>
/// Failures produced while resolving a client's sort/filter request against an
/// endpoint's allow-list. All are <see cref="ErrorType.Validation"/>: they are
/// bad input, not server faults, and must surface as HTTP 400.
/// </summary>
public static class QueryErrors
{
    public static Error UnknownSortField(string field) => Error.Validation(
        "query.sort.unknown_field",
        $"'{field}' cannot be sorted on.");

    public static Error UnknownFilterField(string field) => Error.Validation(
        "query.filter.unknown_field",
        $"'{field}' cannot be filtered on.");

    public static Error UnsupportedOperator(string field, FilterOperator op) => Error.Validation(
        "query.filter.unsupported_operator",
        $"Operator '{op}' cannot be applied to '{field}'.");

    public static Error InvalidValue(string field, string value) => Error.Validation(
        "query.filter.invalid_value",
        $"'{value}' is not a valid value for '{field}'.");
}
