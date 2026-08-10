namespace Erp.Api.Common.Results;

/// <summary>
/// Stands in for "no value" where a generic type argument is required â€”
/// <c>Result&lt;Unit&gt;</c> for a command that changes state and returns nothing.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
