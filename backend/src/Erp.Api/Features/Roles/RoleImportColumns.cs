using Erp.Api.Common.Excel;

namespace Erp.Api.Features.Roles;

/// <summary>
/// Every column of the roles import sheet.
/// <para>
/// This is the <em>legacy</em> role master, which grants nothing â€”
/// authorisation runs on Identity roles. Importing here creates rows that
/// <c>Employee.RoleId</c> can point at; it does not give anybody a permission.
/// The note on <c>Cross business unit</c> is deliberately blunt for that reason.
/// </para>
/// </summary>
public static class RoleImportColumns
{
    public static readonly ImportColumn RoleId = new(
        "Role id",
        ImportColumnKind.WholeNumber,
        Required: true,
        Note: "The legacy role number that Employee rows reference.");

    public static readonly ImportColumn RolesName = new(
        "Roles name",
        Required: true,
        MaxLength: 100,
        Note: "Unique across the whole system. Must not already exist.");

    public static readonly ImportColumn BypassBusinessUnit = new(
        "Cross business unit",
        ImportColumnKind.Boolean,
        Note: "Yes lets holders read every business unit's data. Blank counts as No.");

    public static readonly ImportColumn IsActive = new(
        "Active",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as Yes.");

    public static readonly IReadOnlyList<ImportColumn> All =
    [
        RoleId,
        RolesName,
        BypassBusinessUnit,
        IsActive,
    ];
}
