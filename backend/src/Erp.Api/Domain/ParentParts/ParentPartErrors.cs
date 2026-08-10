using Erp.Api.Common.Results;

namespace Erp.Api.Domain.ParentParts;

/// <summary>Every way a parent part operation can fail, named once.</summary>
public static class ParentPartErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "parent_part.not_found",
        $"No parent part with id '{id}' exists in this business unit.");

    public static Error PartNotFound(Guid partId) => Error.Validation(
        "parent_part.part.not_found",
        $"No part with id '{partId}' exists in this business unit.");

    public static Error AlreadyDefined(string partNumber) => Error.Conflict(
        "parent_part.duplicate",
        $"Part '{partNumber}' already has a build defined. Edit that record instead of creating a second one.");

    public static Error ComponentNotFound(Guid partId) => Error.Validation(
        "parent_part.component.not_found",
        $"No part with id '{partId}' exists in this business unit, so it cannot be used as a component.");

    /// <summary>
    /// The legacy screen had no such check, so the same child could be added to a
    /// parent any number of times and every one of them counted towards the totals.
    /// </summary>
    public static Error DuplicateComponent(string partNumber) => Error.Validation(
        "parent_part.component.duplicate",
        $"Part '{partNumber}' appears more than once. Change its quantity instead of adding a second line.");

    /// <summary>
    /// A part that contains itself explodes forever. Nothing in the legacy screen
    /// prevented it.
    /// </summary>
    public static Error ComponentIsParent(string partNumber) => Error.Validation(
        "parent_part.component.is_parent",
        $"Part '{partNumber}' cannot be a component of itself.");

    public static Error QuantityMustBePositive(string partNumber) => Error.Validation(
        "parent_part.component.quantity",
        $"The quantity for '{partNumber}' must be greater than zero.");

    public static Error StaleRowVersion => Error.Conflict(
        "parent_part.stale_row_version",
        "This parent part was changed by someone else since you loaded it. Reload and try again.");
}
