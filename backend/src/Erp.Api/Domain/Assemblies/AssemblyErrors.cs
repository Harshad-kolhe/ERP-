using Erp.Api.Common.Results;

namespace Erp.Api.Domain.Assemblies;

/// <summary>
/// Every way a section, assembly or sub-assembly operation can fail, named once.
/// <para>
/// The legacy equivalent returned <c>result.AckMsg = "Error: " + ex.Message</c>
/// under HTTP 200, so a client could not tell a duplicate name from a dropped
/// database connection. Each of these carries a stable code and a type that
/// decides the status the caller sees.
/// </para>
/// </summary>
public static class AssemblyErrors
{
    public static Error NotFound(AssemblyLevel level, Guid id) => Error.NotFound(
        "assembly.not_found",
        $"No {AssemblyLevels.Describe(level)} with id '{id}' exists in this business unit.");

    public static Error DuplicateCode(string code) => Error.Conflict(
        "assembly.code.duplicate",
        $"Code '{code}' is already in use by another section, assembly or sub-assembly.");

    public static Error DuplicateName(AssemblyLevel level, string name) => Error.Conflict(
        "assembly.name.duplicate",
        $"{AssemblyLevels.DescribeCapitalised(level)} named '{name}' already exists here.");

    public static Error ParentRequired(AssemblyLevel level) => Error.Validation(
        "assembly.parent.required",
        $"{AssemblyLevels.DescribeCapitalised(level)} must belong to "
        + $"{AssemblyLevels.DescribeWithArticle(AssemblyLevels.ParentOf(level)!.Value)}.");

    public static Error ParentNotAllowed(AssemblyLevel level) => Error.Validation(
        "assembly.parent.not_allowed",
        $"{AssemblyLevels.DescribeCapitalised(level)} sits at the top of the breakdown "
        + "and cannot have a parent.");

    public static Error ParentNotFound(Guid parentId) => Error.Validation(
        "assembly.parent.not_found",
        $"No section, assembly or sub-assembly with id '{parentId}' exists in this business unit.");

    public static Error ParentWrongLevel(AssemblyLevel level, AssemblyLevel actualParentLevel) => Error.Validation(
        "assembly.parent.wrong_level",
        $"{AssemblyLevels.DescribeCapitalised(level)} must belong to "
        + $"{AssemblyLevels.DescribeWithArticle(AssemblyLevels.ParentOf(level)!.Value)}, "
        + $"but the one selected is {AssemblyLevels.DescribeWithArticle(actualParentLevel)}.");

    // No "would create a cycle" error, deliberately. With exactly three levels, a
    // level that never changes, and a parent that must sit exactly one level above,
    // a node cannot become its own ancestor â€” the check would be a guard that can
    // never fire, which reads like protection and provides none. If the breakdown
    // ever gains arbitrary depth, AssemblyLevels is where that changes, and the
    // check belongs here at the same time.

    public static Error HasActiveChildren(int childCount) => Error.Conflict(
        "assembly.has_active_children",
        $"This node still has {childCount} active child node(s). Deactivate or move them first â€” "
        + "otherwise they stay selectable under a parent that is not.");

    public static Error StaleRowVersion => Error.Conflict(
        "assembly.stale_row_version",
        "This record was changed by someone else since you loaded it. Reload and try again.");
}
