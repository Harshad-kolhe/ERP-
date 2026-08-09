using Erp.BuildingBlocks.Web.Security;
using Erp.Contracts.Security;

namespace Erp.Modules.Masters.Integration;

/// <summary>
/// The permission codes this module defines.
/// <para>
/// Constants because endpoints reference them — <c>.RequirePermission(MastersPermissions.PartCreate)</c>
/// — and a typo must be a compile error rather than an endpoint nobody can reach.
/// </para>
/// <para>
/// Note what is deliberately <em>not</em> here: any statement about which role holds
/// these. That mapping is data, editable at runtime through the roles screen, and it
/// lives only in the database. Nothing in this codebase may say "an Administrator
/// can approve parts" — it says "approving a part requires this permission", and an
/// administrator decides who gets it.
/// </para>
/// </summary>
public static class MastersPermissions
{
    /// <summary>
    /// Bulk import is its own permission on every master, never folded into create.
    /// <para>
    /// Creating one record and loading five thousand in one transaction are
    /// different powers. A clerk who maintains the customer list should be able to
    /// add a customer without also being able to replace the master in a single
    /// upload, and the person doing a migration usually needs the second without
    /// needing the first for long.
    /// </para>
    /// </summary>
    public const string PartImport = "masters.part.import";

    public const string SupplierImport = "masters.supplier.import";

    public const string CustomerImport = "masters.customer.import";

    public const string EmployeeImport = "masters.employee.import";

    public const string BusinessUnitImport = "masters.businessunit.import";

    public const string RoleImport = "masters.role.import";

    public const string PartRead = "masters.part.read";

    public const string PartCreate = "masters.part.create";

    public const string PartUpdate = "masters.part.update";

    public const string PartSubmit = "masters.part.submit";

    /// <summary>Separate from <see cref="PartUpdate"/> so approval can be granted independently.</summary>
    public const string PartApprove = "masters.part.approve";

    public const string SupplierRead = "masters.supplier.read";

    public const string SupplierCreate = "masters.supplier.create";

    public const string SupplierUpdate = "masters.supplier.update";

    public const string CustomerRead = "masters.customer.read";

    public const string CustomerCreate = "masters.customer.create";

    public const string CustomerUpdate = "masters.customer.update";

    public const string EmployeeRead = "masters.employee.read";

    public const string EmployeeCreate = "masters.employee.create";

    public const string EmployeeUpdate = "masters.employee.update";

    /// <summary>
    /// Sees the pay columns — PF, ESI, PT, TDS, gross, net and hourly rate.
    /// <para>
    /// Separate from <see cref="EmployeeRead"/> on purpose. Everyone who raises a job
    /// card needs to look an employee up; almost nobody needs to see what they are
    /// paid. The legacy grid put both in one screen behind one check, so anyone who
    /// could find a phone number could read the payroll — and the same grid also
    /// carried a <c>Password</c> column, which is not reproduced here at all.
    /// </para>
    /// <para>
    /// Enforced in <c>ListEmployeesHandler</c>, which both redacts the values and
    /// withholds the fields from its sort and filter map — otherwise
    /// <c>sort=netSalary:desc</c> would reveal the ordering to someone forbidden the
    /// numbers.
    /// </para>
    /// </summary>
    public const string EmployeePayrollRead = "masters.employee.payroll.read";

    /// <summary>
    /// Reads every business unit, not just the caller's own — the business unit table
    /// is not tenant-scoped, so this permission is the whole access control on it.
    /// Grant it deliberately rather than bundling it with the other master reads.
    /// </summary>
    public const string BusinessUnitRead = "masters.businessunit.read";

    public const string BusinessUnitCreate = "masters.businessunit.create";

    public const string BusinessUnitUpdate = "masters.businessunit.update";

    public const string RoleRead = "masters.role.read";

    public const string RoleCreate = "masters.role.create";

    public const string RoleUpdate = "masters.role.update";

    /// <summary>
    /// Sections, assemblies and sub-assemblies are one table but three permissions,
    /// not one.
    /// <para>
    /// The three levels are maintained by different people: sections are set up once
    /// when a machine family is defined and rarely touched, while sub-assemblies
    /// change with every design revision. Granting the draughtsman who edits
    /// sub-assemblies the ability to restructure the section list — which is what a
    /// single <c>masters.assembly.*</c> permission would do — hands out an authority
    /// nobody asked for.
    /// </para>
    /// </summary>
    public const string SectionRead = "masters.section.read";

    public const string SectionCreate = "masters.section.create";

    public const string SectionUpdate = "masters.section.update";

    public const string AssemblyRead = "masters.assembly.read";

    public const string AssemblyCreate = "masters.assembly.create";

    public const string AssemblyUpdate = "masters.assembly.update";

    public const string SubAssemblyRead = "masters.subassembly.read";

    public const string SubAssemblyCreate = "masters.subassembly.create";

    public const string SubAssemblyUpdate = "masters.subassembly.update";

    public const string ParentPartRead = "masters.parentpart.read";

    public const string ParentPartCreate = "masters.parentpart.create";

    public const string ParentPartUpdate = "masters.parentpart.update";

    /// <summary>
    /// Maintains the code lists every other master picks from — lookup values,
    /// units of measure and HSN codes.
    /// <para>
    /// One set of three permissions across all three tables, where sections and
    /// assemblies got a set each. The difference is who does the work: the machine
    /// hierarchy is maintained by different people at each level, whereas the code
    /// lists are one job done by one administrator. Splitting them nine ways would
    /// produce permissions that are always granted together, which teaches everyone
    /// to grant them without reading.
    /// </para>
    /// <para>
    /// Separate from reading a list, which needs no permission at all — every form
    /// in the application has to fill its dropdowns, and that endpoint is
    /// authenticated-only. This is the power to <em>change</em> what the dropdowns
    /// offer, and through them what every master will accept.
    /// </para>
    /// </summary>
    public const string ReferenceDataRead = "masters.referencedata.read";

    public const string ReferenceDataCreate = "masters.referencedata.create";

    public const string ReferenceDataUpdate = "masters.referencedata.update";
}

/// <summary>Publishes this module's permissions to the catalogue the roles screen reads.</summary>
public sealed class MastersPermissionSource : IPermissionSource
{
    public string Module => "Masters";

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new(MastersPermissions.PartRead, "View parts", "Parts", "Masters"),
        new(MastersPermissions.PartCreate, "Create parts", "Parts", "Masters"),
        new(MastersPermissions.PartUpdate, "Edit parts", "Parts", "Masters"),
        new(MastersPermissions.PartSubmit, "Submit parts for approval", "Parts", "Masters"),
        new(MastersPermissions.PartApprove, "Approve parts", "Parts", "Masters"),

        new(MastersPermissions.SupplierRead, "View suppliers", "Suppliers", "Masters"),
        new(MastersPermissions.SupplierCreate, "Create suppliers", "Suppliers", "Masters"),
        new(MastersPermissions.SupplierUpdate, "Edit suppliers", "Suppliers", "Masters"),

        new(MastersPermissions.CustomerRead, "View customers", "Customers", "Masters"),
        new(MastersPermissions.CustomerCreate, "Create customers", "Customers", "Masters"),
        new(MastersPermissions.CustomerUpdate, "Edit customers", "Customers", "Masters"),

        new(MastersPermissions.EmployeeRead, "View employees", "Employees", "Masters"),
        new(MastersPermissions.EmployeeCreate, "Create employees", "Employees", "Masters"),
        new(MastersPermissions.EmployeeUpdate, "Edit employees", "Employees", "Masters"),
        new(MastersPermissions.EmployeePayrollRead, "View employee pay details", "Employees", "Masters"),

        new(MastersPermissions.BusinessUnitRead, "View business units", "Business units", "Masters"),
        new(MastersPermissions.BusinessUnitCreate, "Create business units", "Business units", "Masters"),
        new(MastersPermissions.BusinessUnitUpdate, "Edit business units", "Business units", "Masters"),

        new(MastersPermissions.RoleRead, "View roles", "Roles", "Masters"),
        new(MastersPermissions.RoleCreate, "Create roles", "Roles", "Masters"),
        new(MastersPermissions.RoleUpdate, "Edit roles", "Roles", "Masters"),

        new(MastersPermissions.SectionRead, "View sections", "Sections", "Masters"),
        new(MastersPermissions.SectionCreate, "Create sections", "Sections", "Masters"),
        new(MastersPermissions.SectionUpdate, "Edit sections", "Sections", "Masters"),

        new(MastersPermissions.AssemblyRead, "View assemblies", "Assemblies", "Masters"),
        new(MastersPermissions.AssemblyCreate, "Create assemblies", "Assemblies", "Masters"),
        new(MastersPermissions.AssemblyUpdate, "Edit assemblies", "Assemblies", "Masters"),

        new(MastersPermissions.SubAssemblyRead, "View sub-assemblies", "Sub-assemblies", "Masters"),
        new(MastersPermissions.SubAssemblyCreate, "Create sub-assemblies", "Sub-assemblies", "Masters"),
        new(MastersPermissions.SubAssemblyUpdate, "Edit sub-assemblies", "Sub-assemblies", "Masters"),

        new(MastersPermissions.ParentPartRead, "View parent parts", "Parent parts", "Masters"),
        new(MastersPermissions.ParentPartCreate, "Create parent parts", "Parent parts", "Masters"),
        new(MastersPermissions.ParentPartUpdate, "Edit parent parts", "Parent parts", "Masters"),

        new(MastersPermissions.ReferenceDataRead, "View reference data", "Reference data", "Masters"),
        new(MastersPermissions.ReferenceDataCreate, "Add reference data", "Reference data", "Masters"),
        new(MastersPermissions.ReferenceDataUpdate, "Edit reference data", "Reference data", "Masters"),

        new(MastersPermissions.PartImport, "Import parts from Excel", "Parts", "Masters"),
        new(MastersPermissions.SupplierImport, "Import suppliers from Excel", "Suppliers", "Masters"),
        new(MastersPermissions.CustomerImport, "Import customers from Excel", "Customers", "Masters"),
        new(MastersPermissions.EmployeeImport, "Import employees from Excel", "Employees", "Masters"),
        new(MastersPermissions.BusinessUnitImport, "Import business units from Excel", "Business units", "Masters"),
        new(MastersPermissions.RoleImport, "Import roles from Excel", "Roles", "Masters"),
    ];
}
