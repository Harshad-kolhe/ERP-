using Erp.Api.Common.Excel;

namespace Erp.Api.Features.BusinessUnits;

/// <summary>
/// Every column of the business units import sheet.
/// <para>
/// <c>Unit id</c> is here and is required, unlike the other masters' surrogate
/// keys. Every other table's tenancy column holds this value, so importing a
/// business unit without it produces a tenant that no migrated record can point at.
/// </para>
/// </summary>
public static class BusinessUnitImportColumns
{
    public static readonly ImportColumn BusinessUnitId = new(
        "Unit id",
        ImportColumnKind.WholeNumber,
        Required: true,
        Note: "The number other tables carry in their business unit column. Must not already exist.");

    public static readonly ImportColumn BusinessName = new(
        "Business name",
        Required: true,
        MaxLength: 200,
        Note: "Unique across the whole system, not just within a tenant.");

    public static readonly ImportColumn Address = new("Address", MaxLength: 500);

    public static readonly ImportColumn StateName = new("State name", MaxLength: 100);

    public static readonly ImportColumn StateCode = new("State code", MaxLength: 10);

    public static readonly ImportColumn ContactNumber = new("Contact number", MaxLength: 30);

    public static readonly ImportColumn Email = new("Email", MaxLength: 150);

    public static readonly ImportColumn Website = new("Website", MaxLength: 200);

    public static readonly ImportColumn Cin = new(
        "CIN",
        MaxLength: 21,
        Note: "Corporate Identification Number, 21 characters.");

    public static readonly ImportColumn Gstn = new("GSTN", MaxLength: 15);

    public static readonly ImportColumn Pan = new("PAN", MaxLength: 10);

    public static readonly ImportColumn IsActive = new(
        "Active",
        ImportColumnKind.Boolean,
        Note: "Yes or No. Blank counts as Yes.");

    public static readonly IReadOnlyList<ImportColumn> All =
    [
        BusinessUnitId,
        BusinessName,
        Address,
        StateName,
        StateCode,
        ContactNumber,
        Email,
        Website,
        Cin,
        Gstn,
        Pan,
        IsActive,
    ];
}
