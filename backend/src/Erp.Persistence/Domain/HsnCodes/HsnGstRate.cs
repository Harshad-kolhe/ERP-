using Erp.SharedKernel.Primitives;

namespace Erp.Persistence.Domain.HsnCodes;

/// <summary>
/// The GST rate an <see cref="HsnCode"/> attracted from a given date.
/// <para>
/// Not an aggregate root, and not addressable on its own: a rate has no meaning
/// apart from the code it belongs to, and it is only ever reached through one.
/// </para>
/// <para>
/// A rate is never edited in place. The Council changes rates, and an invoice
/// raised last March must still be explainable at the rate that applied last
/// March — so a change is a new row with a later <see cref="EffectiveFrom"/> and
/// the old row stays exactly as it was. Overwriting a single rate column, which is
/// what a flat master would do, silently rewrites the tax on every historical
/// document that reads it.
/// </para>
/// </summary>
public sealed class HsnGstRate : Entity<int>
{
    /// <summary>For EF materialisation only.</summary>
    private HsnGstRate()
    {
    }

    internal HsnGstRate(decimal ratePercent, DateOnly effectiveFrom)
    {
        RatePercent = ratePercent;
        EffectiveFrom = effectiveFrom;
    }

    /// <summary>Owning code. A real foreign key, with cascade delete.</summary>
    public int HsnCodeId { get; private set; }

    /// <summary>
    /// The total GST percentage — 18 for eighteen percent.
    /// <para>
    /// One number, not three. CGST, SGST and IGST are all derived from it: an
    /// intra-state supply splits it in half across CGST and SGST, an inter-state one
    /// charges the whole as IGST. Storing the three separately gives three columns
    /// that can disagree about a single rate.
    /// </para>
    /// </summary>
    public decimal RatePercent { get; private set; }

    /// <summary>
    /// The date the rate took effect. A date rather than an instant, because a rate
    /// change is announced for a day, not for a moment in a timezone.
    /// </summary>
    public DateOnly EffectiveFrom { get; private set; }
}
