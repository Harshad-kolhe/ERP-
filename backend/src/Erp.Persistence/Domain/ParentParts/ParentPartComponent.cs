using Erp.Persistence.Domain.Parts;
using Erp.SharedKernel.Primitives;

namespace Erp.Persistence.Domain.ParentParts;

/// <summary>
/// One component line of a <see cref="ParentPart"/>: a part, how many of it, and
/// what that quantity weighs and costs.
/// <para>
/// Not an aggregate root. A line has no meaning apart from the build it belongs
/// to, so it is only ever reached through <see cref="ParentPart"/> and is created,
/// replaced and deleted with it in one transaction. The legacy system made the
/// opposite choice — lines were rows in the same table as headers, addressable by
/// their own endpoints — and the result was that adding a child and updating the
/// header's totals were two separate writes that could, and did, disagree.
/// </para>
/// </summary>
public sealed class ParentPartComponent : Entity<Guid>
{
    /// <summary>Scale quantities are stored at, matching <c>Quantity.Scale</c>.</summary>
    internal const int QuantityScale = 6;

    /// <summary>Scale weights are stored at, matching <c>Part.WeightKg</c>.</summary>
    internal const int WeightScale = 4;

    /// <summary>Scale money is stored at, matching <c>Money.Scale</c>.</summary>
    internal const int MoneyScale = 4;

    /// <summary>For EF materialisation only.</summary>
    private ParentPartComponent()
    {
    }

    private ParentPartComponent(Guid id, PartId partId, decimal quantity)
        : base(id)
    {
        PartId = partId;
        Quantity = quantity;
    }

    /// <summary>Owning build. A real foreign key, with cascade delete.</summary>
    public ParentPartId ParentPartId { get; private set; }

    /// <summary>The component part. A real foreign key onto the part master.</summary>
    public PartId PartId { get; private set; }

    /// <summary>How many of the component one parent takes. Always greater than zero.</summary>
    public decimal Quantity { get; private set; }

    public string? UnitOfMeasureCode { get; private set; }

    /// <summary>Kilograms per unit of the component.</summary>
    public decimal? UnitWeightKg { get; private set; }

    /// <summary>Cost per unit of the component.</summary>
    public decimal? Rate { get; private set; }

    /// <summary>
    /// Quantity × <see cref="Rate"/>, maintained here and never accepted from the
    /// caller — a stored total anybody can post an arbitrary value into is not a
    /// total, and the header sums this column.
    /// </summary>
    public decimal? Amount { get; private set; }

    /// <summary>Quantity × <see cref="UnitWeightKg"/>, on the same terms as <see cref="Amount"/>.</summary>
    public decimal? LineWeightKg { get; private set; }

    public string? DrawingNumber { get; private set; }

    public string? Remark { get; private set; }

    /// <summary>
    /// Position in the build, from 1. Stored so the lines come back in the order
    /// the user arranged them in — without it the order is whatever the index
    /// happens to give, which changes as lines are edited.
    /// </summary>
    public int LineNumber { get; private set; }

    internal static ParentPartComponent Create(
        PartId partId,
        decimal quantity,
        int lineNumber,
        string? unitOfMeasureCode,
        decimal? unitWeightKg,
        decimal? rate,
        string? drawingNumber,
        string? remark)
    {
        var component = new ParentPartComponent(Guid.CreateVersion7(), partId, Round(quantity, QuantityScale))
        {
            LineNumber = lineNumber,
            UnitOfMeasureCode = CleanCode(unitOfMeasureCode),
            UnitWeightKg = Round(unitWeightKg, WeightScale),
            Rate = Round(rate, MoneyScale),
            DrawingNumber = Clean(drawingNumber),
            Remark = Clean(remark),
        };

        component.Recalculate();

        return component;
    }

    /// <summary>Recomputes the two derived columns from the entered ones.</summary>
    private void Recalculate()
    {
        Amount = Rate is null ? null : Round(Quantity * Rate.Value, MoneyScale);
        LineWeightKg = UnitWeightKg is null ? null : Round(Quantity * UnitWeightKg.Value, WeightScale);
    }

    private static decimal Round(decimal value, int scale) =>
        decimal.Round(value, scale, MidpointRounding.AwayFromZero);

    private static decimal? Round(decimal? value, int scale) =>
        value is null ? null : Round(value.Value, scale);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? CleanCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
