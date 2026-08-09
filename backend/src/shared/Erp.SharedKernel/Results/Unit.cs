namespace Erp.SharedKernel.Results;

/// <summary>
/// Stands in for "no value" where a generic type argument is required —
/// <c>Result&lt;Unit&gt;</c> for a command that changes state and returns nothing.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
