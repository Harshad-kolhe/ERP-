using Erp.Api.Domain.UnitsOfMeasure;

namespace Erp.UnitTests.Domain;

/// <summary>
/// Conversion is the reason a unit is a master rather than a dropdown option, so
/// it is the thing worth a test.
/// </summary>
public sealed class UnitOfMeasureTests
{
    [Fact]
    public void A_base_unit_converts_to_itself_unchanged()
    {
        var kg = Unit("KG");

        UnitOfMeasure.ConvertQuantity(kg, kg, 12.5m).Value.ShouldBe(12.5m);
    }

    [Fact]
    public void Converting_down_to_the_base_unit_multiplies_by_the_factor()
    {
        var result = UnitOfMeasure.ConvertQuantity(Unit("TON", "KG", 1000m), Unit("KG"), 2m);

        result.Value.ShouldBe(2000m);
    }

    [Fact]
    public void Converting_up_from_the_base_unit_divides_by_the_factor()
    {
        var result = UnitOfMeasure.ConvertQuantity(Unit("KG"), Unit("TON", "KG", 1000m), 2500m);

        result.Value.ShouldBe(2.5m);
    }

    /// <summary>
    /// The case that matters. A helper that returned the number unchanged here would
    /// put a weight into a length field and nothing downstream would notice.
    /// </summary>
    [Fact]
    public void There_is_no_conversion_between_units_that_measure_different_things()
    {
        var result = UnitOfMeasure.ConvertQuantity(Unit("KG"), Unit("MTR"), 5m);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("uom.not_convertible");
    }

    private static UnitOfMeasure Unit(string code, string? baseUnitCode = null, decimal? conversionToBase = null) =>
        new()
        {
            Code = code,
            Name = code,
            BaseUnitCode = baseUnitCode,
            ConversionToBase = conversionToBase,
        };
}
