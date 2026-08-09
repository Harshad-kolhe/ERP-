using Erp.SharedKernel.Primitives;

namespace Erp.Persistence.Domain.HsnCodes;

/// <summary>
/// An HSN code, and the GST rates it has attracted over time.
/// <para>
/// Promoted out of free text on <c>Part</c> because the code is not the useful
/// part — the rate is. Validating the shape (4, 6 or 8 digits) tells you a code is
/// plausible; it does not tell you the code exists, and it does not tell an invoice
/// what tax to charge. Both of those need a master, and the rate needs to be
/// effective-dated or history is rewritten every time a rate changes. See
/// <see cref="HsnGstRate"/>.
/// </para>
/// <para>
/// Not <see cref="IBusinessUnitScoped"/>: the GST schedule is national.
/// </para>
/// </summary>
public sealed class HsnCode : AggregateRoot<int>, IAuditable, ISoftDeletable, IHasRowVersion
{
    private readonly List<HsnGstRate> _rates = [];

    /// <summary>4, 6 or 8 digits under the Indian GST schedule.</summary>
    public string Code { get; set; } = null!;

    /// <summary>What the code covers, in the words of the schedule.</summary>
    public string Description { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Every rate this code has attracted, past and present. Exposed read-only and
    /// loaded through the backing field, so a rate cannot be added to a code except
    /// through <see cref="AddRate"/>.
    /// </summary>
    public IReadOnlyCollection<HsnGstRate> Rates => _rates.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// The rate in force on a given date, or null if the code had no rate yet.
    /// <para>
    /// The latest rate that had already taken effect — not the latest rate on the
    /// code. A rate announced for next quarter is already a row here, and an invoice
    /// raised today must not read it.
    /// </para>
    /// </summary>
    /// <param name="on">
    /// The document's own date, supplied by the caller. Reading an ambient clock
    /// here would make a reprinted invoice show today's tax instead of its own.
    /// </param>
    public decimal? RatePercentOn(DateOnly on) =>
        _rates
            .Where(rate => rate.EffectiveFrom <= on)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .Select(rate => (decimal?)rate.RatePercent)
            .FirstOrDefault();

    public void AddRate(decimal ratePercent, DateOnly effectiveFrom) =>
        _rates.Add(new HsnGstRate(ratePercent, effectiveFrom));
}
