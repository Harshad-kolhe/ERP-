using Erp.Modules.Masters.Domain.ParentParts;
using Erp.Modules.Masters.Domain.Parts;

namespace Erp.Modules.Masters.Tests.Domain;

/// <summary>
/// The rollup rules for a parent part.
/// <para>
/// The legacy screen recalculated the header totals after each child insert and
/// wrote the answer onto whichever row carried the parent's number in its
/// <em>child</em> column — usually no row at all, so the totals silently stayed at
/// whatever they were first saved as. It also took each line's amount from the
/// browser, so a line could be stored whose amount disagreed with its own quantity
/// and rate. Both of those are now arithmetic the aggregate owns, which makes them
/// testable without a database.
/// </para>
/// </summary>
public sealed class ParentPartTests
{
    private static readonly PartId Parent = PartId.New();

    [Fact]
    public void A_line_computes_its_own_amount_and_weight()
    {
        var build = Build(Component(quantity: 3m, unitWeightKg: 2.5m, rate: 100m));
        var line = build.Components.Single();

        line.Amount.ShouldBe(300m);
        line.LineWeightKg.ShouldBe(7.5m);
    }

    [Fact]
    public void The_header_totals_are_summed_from_the_lines()
    {
        var build = Build(
            Component(quantity: 2m, unitWeightKg: 1.5m, rate: 50m),
            Component(quantity: 4m, unitWeightKg: 0.25m, rate: 10m));

        build.TotalWeightKg.ShouldBe(4m);
        build.TotalAmount.ShouldBe(140m);
    }

    [Fact]
    public void A_line_with_no_rate_contributes_nothing_to_the_amount()
    {
        // Null is "nobody has priced this yet", which is not the same as zero — but
        // it must not make the whole total null either, or a half-priced build has
        // no total at all.
        var build = Build(
            Component(quantity: 2m, unitWeightKg: 1m, rate: null),
            Component(quantity: 1m, unitWeightKg: 1m, rate: 25m));

        build.Components[0].Amount.ShouldBeNull();
        build.TotalAmount.ShouldBe(25m);
        build.TotalWeightKg.ShouldBe(3m);
    }

    [Fact]
    public void Lines_are_numbered_in_the_order_they_arrive()
    {
        var build = Build(Component(), Component(), Component());

        build.Components.Select(component => component.LineNumber).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Update_replaces_the_whole_component_list_and_recomputes_the_totals()
    {
        var build = Build(
            Component(quantity: 10m, unitWeightKg: 1m, rate: 5m),
            Component(quantity: 10m, unitWeightKg: 1m, rate: 5m));

        build.TotalAmount.ShouldBe(100m);

        build.Update(
            assemblyNodeId: null,
            description: null,
            unitOfMeasureCode: null,
            drawingNumber: null,
            category: null,
            isActive: true,
            components: [Component(quantity: 1m, unitWeightKg: 2m, rate: 3m)]);

        build.Components.Count.ShouldBe(1);
        build.TotalAmount.ShouldBe(3m);
        build.TotalWeightKg.ShouldBe(2m);
    }

    [Fact]
    public void Emptying_the_component_list_zeroes_the_totals()
    {
        var build = Build(Component(quantity: 5m, unitWeightKg: 1m, rate: 1m));

        build.Update(null, null, null, null, null, isActive: true, components: []);

        build.Components.ShouldBeEmpty();
        build.TotalWeightKg.ShouldBe(0m);
        build.TotalAmount.ShouldBe(0m);
    }

    [Fact]
    public void A_build_starts_active_and_can_be_withdrawn()
    {
        var build = Build(Component());

        build.IsActive.ShouldBeTrue();

        build.Update(null, null, null, null, null, isActive: false, components: [Component()]);

        build.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Codes_are_upper_cased_and_free_text_is_only_trimmed()
    {
        var build = ParentPart.Create(
            Parent,
            assemblyNodeId: null,
            description: "  Welded frame  ",
            unitOfMeasureCode: " nos ",
            drawingNumber: "  DRW-1  ",
            category: " fabricated ",
            components: []);

        build.UnitOfMeasureCode.ShouldBe("NOS");
        build.Category.ShouldBe("fabricated");
        build.Description.ShouldBe("Welded frame");
        build.DrawingNumber.ShouldBe("DRW-1");
    }

    private static ParentPart Build(params ParentPartComponentDraft[] components) =>
        ParentPart.Create(Parent, null, null, null, null, null, components);

    private static ParentPartComponentDraft Component(
        decimal quantity = 1m,
        decimal? unitWeightKg = null,
        decimal? rate = null) =>
        new(PartId.New(), quantity, "NOS", unitWeightKg, rate, null, null);
}
