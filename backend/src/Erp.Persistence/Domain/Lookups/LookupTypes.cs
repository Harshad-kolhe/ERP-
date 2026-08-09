namespace Erp.Persistence.Domain.Lookups;

/// <summary>
/// The list names clients ask for. Constants because a typo must be a compile
/// error rather than a dropdown that is silently empty.
/// <para>
/// Dotted and prefixed by the master that owns the list, except where the list is
/// genuinely shared — <see cref="UnitOfMeasure"/> and <see cref="Currency"/> mean
/// the same thing on a part, a supplier and a customer, and duplicating them per
/// master is how three screens end up offering three different sets of units.
/// </para>
/// </summary>
public static class LookupTypes
{
    public const string UnitOfMeasure = "uom";

    public const string Currency = "currency";

    public const string Country = "country";

    public const string PartCategoryCode = "part.categoryCode";

    public const string PartType = "part.type";

    public const string PartFormCategory = "part.formCategory";

    public const string PartMaterialType = "part.materialType";

    public const string PartSeriesCode = "part.seriesCode";

    public const string PartSourceCode = "part.sourceCode";

    public const string PartRevisionNo = "part.revisionNo";

    public const string MaterialOfConstruction = "moc";

    public const string SupplierType = "supplier.type";

    public const string PaymentTerms = "paymentTerms";

    public const string TaxCode = "taxCode";

    public const string CustomerIndustry = "customer.industry";

    public const string EmployeeGender = "employee.gender";

    public const string EmployeeDepartment = "employee.department";

    public const string EmployeeDesignation = "employee.designation";

    public const string EmployeeBloodGroup = "employee.bloodGroup";

    public const string EmployeeQualification = "employee.qualification";

    public const string IndianState = "state";

    /// <summary>
    /// Which machine family a section, assembly or sub-assembly belongs to.
    /// <para>
    /// Shared by all three levels because it is one list — the legacy screens each
    /// carried their own hard-coded array, which is how the same machine ended up
    /// spelled three ways.
    /// </para>
    /// </summary>
    public const string AssemblyMachineType = "assembly.machineType";

    /// <summary>What powers an assembly — motor, hydraulic, pneumatic, manual.</summary>
    public const string AssemblyDrivenBy = "assembly.drivenBy";

    /// <summary>
    /// The approval lifecycle. Served from the same endpoint as the stored lists so
    /// a form has one way of getting its options, but sourced from the enum rather
    /// than from a table — a status is a code path, and a row that invented a fifth
    /// one would reach a switch that cannot handle it.
    /// </summary>
    public const string MasterStatus = "masterStatus";
}
