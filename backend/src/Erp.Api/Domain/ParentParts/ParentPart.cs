using Erp.Api.Domain.Assemblies;
using Erp.Api.Domain.Parts;
using Erp.Api.Common.Entities;

namespace Erp.Api.Domain.ParentParts;

/// <summary>
/// A part that is built from other parts, with the component lines that make it up.
/// <para>
/// This is the legacy <c>AssemblyMaster</c> screen, rebuilt. That table stored the
/// header and its child lines as rows of the same table, told apart by whether
/// <c>ChildPart</c> was null, and then wrote a second copy of every row into
/// <c>AssemblyPartMaster</c> â€” two tables holding the same facts with nothing
/// keeping them in step. Both sides of the relationship were part <em>numbers</em>
/// stored as free text with no foreign key, and the totals were recalculated after
/// each insert onto whichever row carried the parent's number in its <em>child</em>
/// column, which is usually no row at all.
/// </para>
/// <para>
/// Here the header is one row, the lines are its children, the parts are real
/// foreign keys, and the totals are derived by <see cref="Recalculate"/> whenever
/// the lines change â€” so they cannot be wrong and cannot be posted from a browser.
/// </para>
/// </summary>
public sealed class ParentPart
    : AggregateRoot<ParentPartId>, IAuditable, ISoftDeletable, IBusinessUnitScoped, IHasRowVersion
{
    private readonly List<ParentPartComponent> _components = [];

    /// <summary>For EF materialisation only.</summary>
    private ParentPart()
    {
    }

    private ParentPart(ParentPartId id, PartId partId)
        : base(id)
    {
        PartId = partId;
    }

    /// <summary>
    /// The part this record describes the build of. Unique per business unit: one
    /// part has one build, and a second record for the same part would mean two
    /// answers to "what goes into this?".
    /// </summary>
    public PartId PartId { get; private set; }

    /// <summary>Legacy <c>AssemblyDesc</c>. Blank falls back to the part's own description in the UI.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Which section, assembly or sub-assembly this build belongs to.
    /// <para>
    /// The legacy column was a free-text <c>AssemblyCode</c>, so it could name a
    /// code that had been renamed or never existed. Here it is a foreign key onto
    /// <see cref="AssemblyNode"/> and is optional â€” a bought-out sub-assembly need
    /// not sit anywhere in the machine breakdown.
    /// </para>
    /// </summary>
    public AssemblyNodeId? AssemblyNodeId { get; private set; }

    public string? UnitOfMeasureCode { get; private set; }

    public string? DrawingNumber { get; private set; }

    public string? Category { get; private set; }

    /// <summary>
    /// Sum of every line's weight. Maintained by <see cref="Recalculate"/>.
    /// <para>
    /// Persisted rather than computed at read time so the grid can sort and filter
    /// on it in the database, which is the whole point of the list contract. It is
    /// only ever written from the lines, so it cannot drift from them.
    /// </para>
    /// </summary>
    public decimal TotalWeightKg { get; private set; }

    /// <summary>Sum of every line's amount, on the same terms as <see cref="TotalWeightKg"/>.</summary>
    public decimal TotalAmount { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>The component lines, in line-number order. Replaced wholesale â€” see <see cref="ReplaceComponents"/>.</summary>
    public IReadOnlyList<ParentPartComponent> Components => _components;

    public int BusinessUnitId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Creates a build for a part. That the part exists, and that it has no build
    /// already, is checked by the handler â€” the only layer that can see other rows.
    /// </summary>
    public static ParentPart Create(
        PartId partId,
        AssemblyNodeId? assemblyNodeId,
        string? description,
        string? unitOfMeasureCode,
        string? drawingNumber,
        string? category,
        IEnumerable<ParentPartComponentDraft> components)
    {
        var parentPart = new ParentPart(ParentPartId.New(), partId);

        parentPart.ApplyHeader(assemblyNodeId, description, unitOfMeasureCode, drawingNumber, category);
        parentPart.ReplaceComponents(components);

        return parentPart;
    }

    /// <summary>
    /// Applies an edit to the header and replaces the component list in one step,
    /// so the totals are recomputed exactly once and the record is never observed
    /// with new lines and an old total.
    /// </summary>
    public void Update(
        AssemblyNodeId? assemblyNodeId,
        string? description,
        string? unitOfMeasureCode,
        string? drawingNumber,
        string? category,
        bool isActive,
        IEnumerable<ParentPartComponentDraft> components)
    {
        ApplyHeader(assemblyNodeId, description, unitOfMeasureCode, drawingNumber, category);
        IsActive = isActive;
        ReplaceComponents(components);
    }

    /// <summary>
    /// Swaps the whole component list for a new one.
    /// <para>
    /// Wholesale replacement rather than a diff of adds, edits and removes: the
    /// edit screen submits the list it is showing, and reconciling that against
    /// stored rows in the handler is the kind of code that quietly loses a line.
    /// The rows are few â€” a build is tens of lines, not thousands â€” so deleting and
    /// re-inserting them costs nothing measurable and cannot half-apply.
    /// </para>
    /// </summary>
    private void ReplaceComponents(IEnumerable<ParentPartComponentDraft> components)
    {
        _components.Clear();

        var lineNumber = 1;

        foreach (var draft in components)
        {
            _components.Add(ParentPartComponent.Create(
                draft.PartId,
                draft.Quantity,
                lineNumber++,
                draft.UnitOfMeasureCode,
                draft.UnitWeightKg,
                draft.Rate,
                draft.DrawingNumber,
                draft.Remark));
        }

        Recalculate();
    }

    private void ApplyHeader(
        AssemblyNodeId? assemblyNodeId,
        string? description,
        string? unitOfMeasureCode,
        string? drawingNumber,
        string? category)
    {
        AssemblyNodeId = assemblyNodeId;
        Description = Clean(description);
        UnitOfMeasureCode = CleanCode(unitOfMeasureCode);
        DrawingNumber = Clean(drawingNumber);
        Category = Clean(category);
    }

    /// <summary>Rolls the line totals up onto the header. The only writer of the two total columns.</summary>
    private void Recalculate()
    {
        TotalWeightKg = _components.Sum(component => component.LineWeightKg ?? 0m);
        TotalAmount = _components.Sum(component => component.Amount ?? 0m);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? CleanCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

/// <summary>
/// One requested component line, before the domain has turned it into a
/// <see cref="ParentPartComponent"/>.
/// <para>
/// It exists so the aggregate takes its own shape rather than a contract DTO: the
/// derived columns (<c>Amount</c>, <c>LineWeightKg</c>) are on the DTO because the
/// client reads them, and accepting that DTO straight into the domain would mean
/// the domain had to remember to ignore two of its fields.
/// </para>
/// </summary>
public sealed record ParentPartComponentDraft(
    PartId PartId,
    decimal Quantity,
    string? UnitOfMeasureCode,
    decimal? UnitWeightKg,
    decimal? Rate,
    string? DrawingNumber,
    string? Remark);
