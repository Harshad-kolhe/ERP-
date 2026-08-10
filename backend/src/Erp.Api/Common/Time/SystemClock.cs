using Erp.Api.Common.Time;

namespace Erp.Api.Common.Time;

/// <summary>
/// The production <see cref="IClock"/>, backed by <see cref="TimeProvider"/> so
/// tests can substitute <c>FakeTimeProvider</c> without touching production code.
/// </summary>
/// <param name="timeProvider">Injected; <see cref="TimeProvider.System"/> at runtime.</param>
/// <param name="businessTimeZone">
/// Timezone used to answer <see cref="Today"/>. Timestamps are always stored UTC;
/// this only decides which calendar day a document belongs to. It matters at the
/// boundaries: a goods receipt entered at 02:00 IST is the previous day in UTC, and
/// posting it to the wrong day moves stock into the wrong reporting period.
/// </param>
public sealed class SystemClock(TimeProvider timeProvider, TimeZoneInfo businessTimeZone) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), businessTimeZone).DateTime);
}
