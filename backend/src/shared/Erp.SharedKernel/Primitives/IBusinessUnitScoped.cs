namespace Erp.SharedKernel.Primitives;

/// <summary>
/// Marks an entity that belongs to exactly one business unit — the tenancy
/// dimension of this system.
/// <para>
/// A global query filter and a stamping interceptor are applied to every type
/// implementing this interface, by convention, in <c>ErpDbContextBase</c>.
/// This is deliberately not opt-in: the legacy system isolated business units
/// with a hand-called <c>.ApplyBu()</c> LINQ extension, and every query where a
/// developer forgot it leaked another tenant's data. SQL Server row-level
/// security backs it up for any path that bypasses EF entirely.
/// </para>
/// </summary>
public interface IBusinessUnitScoped
{
    int BusinessUnitId { get; set; }
}
