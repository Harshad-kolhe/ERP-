namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>
/// The comparison operators a client may request. A closed set, deliberately:
/// anything a caller sends that is not on this list is rejected, never passed
/// through to the database.
/// </summary>
public enum FilterOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
}
