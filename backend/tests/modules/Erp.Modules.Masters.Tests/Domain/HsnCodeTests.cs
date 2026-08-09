using Erp.Persistence.Domain.HsnCodes;

namespace Erp.Modules.Masters.Tests.Domain;

/// <summary>
/// Effective dating is the reason an HSN code is a master rather than a validated
/// string, so it is the thing worth a test.
/// </summary>
public sealed class HsnCodeTests
{
    private static readonly DateOnly GstStart = new(2017, 7, 1);
    private static readonly DateOnly RateChange = new(2025, 9, 22);

    [Fact]
    public void A_code_with_no_rate_yet_has_no_rate()
    {
        NewCode().RatePercentOn(GstStart).ShouldBeNull();
    }

    [Fact]
    public void A_document_dated_before_a_rate_change_reads_the_old_rate()
    {
        var code = NewCode();
        code.AddRate(28m, GstStart);
        code.AddRate(18m, RateChange);

        code.RatePercentOn(RateChange.AddDays(-1)).ShouldBe(28m);
    }

    [Fact]
    public void A_document_dated_on_the_day_a_rate_takes_effect_reads_the_new_rate()
    {
        var code = NewCode();
        code.AddRate(28m, GstStart);
        code.AddRate(18m, RateChange);

        code.RatePercentOn(RateChange).ShouldBe(18m);
    }

    /// <summary>
    /// A rate announced for a future date is already a row. An invoice raised today
    /// must not find it — which is what taking the latest rate on the code, rather
    /// than the latest one in force, would do.
    /// </summary>
    [Fact]
    public void A_rate_that_has_not_taken_effect_yet_is_not_used()
    {
        var code = NewCode();
        code.AddRate(18m, GstStart);
        code.AddRate(5m, RateChange);

        code.RatePercentOn(GstStart.AddYears(1)).ShouldBe(18m);
    }

    /// <summary>Rows arrive from the database in no guaranteed order.</summary>
    [Fact]
    public void The_rate_in_force_does_not_depend_on_the_order_rates_were_added()
    {
        var code = NewCode();
        code.AddRate(18m, RateChange);
        code.AddRate(28m, GstStart);

        code.RatePercentOn(RateChange).ShouldBe(18m);
    }

    private static HsnCode NewCode() => new() { Code = "84821011", Description = "Ball bearings, radial" };
}
