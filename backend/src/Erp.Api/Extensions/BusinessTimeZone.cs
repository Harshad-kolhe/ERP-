namespace Erp.Api.Extensions;

/// <summary>
/// Resolves the timezone that decides which calendar day a document belongs to.
/// <para>
/// Timestamps are always stored UTC. This only answers "what is today?" — which
/// matters at the edges: a goods receipt entered at 02:00 IST is still the previous
/// day in UTC, and posting it to the wrong day moves stock into the wrong period.
/// </para>
/// </summary>
internal static class BusinessTimeZone
{
    private const string DefaultTimeZoneId = "Asia/Kolkata";

    public static TimeZoneInfo Resolve(IConfiguration configuration)
    {
        var id = configuration["Localization:BusinessTimeZone"] ?? DefaultTimeZoneId;

        try
        {
            // .NET resolves IANA ids on Windows too, via ICU.
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back rather than refuse to start: a misconfigured timezone should
            // not take the application down, but it must be visible.
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
