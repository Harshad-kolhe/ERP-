using Erp.Api.Common.Entities;

namespace Erp.Api.Domain.Lookups;

/// <summary>
/// One option in a dropdown, stored rather than written into a screen.
/// <para>
/// This table exists so that no list of choices lives in source. The legacy system
/// had both problems at once: some lists came from a category master and some were
/// typed straight into the JavaScript â€” <c>items: ["OutSource", "In House"]</c> â€”
/// so adding a source code meant a deployment, and the two halves disagreed about
/// what a valid value was. Every enumerable field in this module now reads from
/// here, and adding an option is a row.
/// </para>
/// <para>
/// Deliberately not <see cref="IBusinessUnitScoped"/>. A unit of measure means the
/// same thing in every business unit, and scoping reference data multiplies it per
/// tenant for no gain â€” the first person to add "KG" in one unit would find it
/// missing in the next.
/// </para>
/// </summary>
public sealed class LookupValue : AggregateRoot<int>, IAuditable, ISoftDeletable, IHasRowVersion
{
    /// <summary>Which list this belongs to â€” see <see cref="LookupTypes"/>.</summary>
    public string Type { get; set; } = null!;

    /// <summary>What gets stored on the record that references it.</summary>
    public string Code { get; set; } = null!;

    /// <summary>What the user sees. Equal to <see cref="Code"/> for lists where the code is the label.</summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Display order within the list. Explicit because these lists have a natural
    /// order that is neither alphabetical nor insertion order â€” a UOM list wants
    /// NOS first, not AMP.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Retired options stay in the table and drop out of the dropdown. Deleting
    /// them would leave existing records pointing at a code nothing can explain.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
