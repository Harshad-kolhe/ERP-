namespace Erp.SharedKernel.Time;

/// <summary>
/// The only sanctioned source of "now".
/// <para>
/// <c>DateTime.Now</c>, <c>DateTime.UtcNow</c> and friends are banned APIs
/// (see <c>BannedSymbols.txt</c>): ambient time is untestable, and local time is
/// wrong the moment the application runs anywhere but one office. Everything is
/// stored in UTC and converted at the presentation edge.
/// </para>
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Today's date in the application's business timezone, for financial-year and document-date logic.</summary>
    DateOnly Today { get; }
}
