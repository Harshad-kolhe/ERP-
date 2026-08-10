namespace Erp.Api.Common.Security;

/// <summary>
/// The tenancy dimension for the current request.
/// <para>
/// Read by the global query filter applied to every <see cref="Erp.Api.Common.Entities.IBusinessUnitScoped"/>
/// entity, and by the interceptor that stamps the business unit on insert. No
/// handler calls this directly to filter a query â€” that was the legacy model, and
/// every query where someone forgot leaked another unit's data.
/// </para>
/// </summary>
public interface IBusinessUnitContext
{
    /// <summary>The business unit whose data this request may see.</summary>
    int BusinessUnitId { get; }

    /// <summary>
    /// True for principals allowed to read across every business unit.
    /// When true the global query filter is not applied.
    /// </summary>
    bool CanAccessAllBusinessUnits { get; }
}
