namespace Erp.Contracts.Masters;

/// <summary>
/// One component line of a parent part: which part goes in, how many, and what it
/// weighs and costs.
/// </summary>
public sealed record ParentPartComponentDto
{
    /// <summary>The component part. The only field the caller must supply besides the quantity.</summary>
    public required Guid PartId { get; init; }

    /// <summary>Resolved server-side. Ignored on the way in — the id is what identifies the part.</summary>
    public string? PartNumber { get; init; }

    public string? PartDescription { get; init; }

    /// <summary>
    /// How many of this component one parent takes. Must be greater than zero: a
    /// line with a quantity of nothing is a line that should have been deleted, and
    /// leaving it in means a BOM explosion multiplies by zero somewhere downstream.
    /// </summary>
    public required decimal Quantity { get; init; }

    public string? UnitOfMeasureCode { get; init; }

    /// <summary>Kilograms per unit of the component.</summary>
    public decimal? UnitWeightKg { get; init; }

    /// <summary>Cost per unit of the component.</summary>
    public decimal? Rate { get; init; }

    /// <summary>
    /// Quantity × rate, computed by the server and never accepted from the client.
    /// <para>
    /// The legacy screen took the amount from the browser, which meant a hand-edited
    /// request could store a line whose amount did not match its own quantity and
    /// rate — and the header totals were summed from exactly that column.
    /// </para>
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>Quantity × unit weight, computed by the server.</summary>
    public decimal? LineWeightKg { get; init; }

    public string? DrawingNumber { get; init; }

    public string? Remark { get; init; }
}
