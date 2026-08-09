using Erp.Modules.Masters.Domain.Assemblies;

namespace Erp.Modules.Masters.Tests.Domain;

/// <summary>
/// The rules that decide the shape of the machine breakdown.
/// <para>
/// These are the rules the legacy system spread across three save methods that had
/// already drifted: sections validated nothing, assemblies checked their parent was
/// a section, and sub-assemblies accepted a section <em>or</em> an assembly plus a
/// further condition whose outcome depended on the order records were entered in.
/// Here there is one table of allowed parents and one place that applies it, so the
/// answer to "what may sit under what" is a test rather than an archaeology
/// exercise.
/// </para>
/// </summary>
public sealed class AssemblyNodeTests
{
    private static readonly AssemblyNodeId Parent = AssemblyNodeId.New();

    /// <summary>
    /// Written as one fact rather than a theory because <see cref="AssemblyLevel"/>
    /// is <c>internal</c> — a theory's parameters are part of a public method
    /// signature, and the module's types are deliberately not visible that far.
    /// </summary>
    [Fact]
    public void Each_level_names_the_level_directly_above_it()
    {
        AssemblyLevels.ParentOf(AssemblyLevel.Section).ShouldBeNull();
        AssemblyLevels.ParentOf(AssemblyLevel.Assembly).ShouldBe(AssemblyLevel.Section);
        AssemblyLevels.ParentOf(AssemblyLevel.SubAssembly).ShouldBe(AssemblyLevel.Assembly);

        AssemblyLevels.RequiresParent(AssemblyLevel.Section).ShouldBeFalse();
        AssemblyLevels.RequiresParent(AssemblyLevel.Assembly).ShouldBeTrue();
        AssemblyLevels.RequiresParent(AssemblyLevel.SubAssembly).ShouldBeTrue();
    }

    [Fact]
    public void A_section_is_created_without_a_parent()
    {
        var created = AssemblyNode.Create(AssemblyLevel.Section, null, "S1", "Frame");

        created.IsSuccess.ShouldBeTrue();
        created.Value.ParentId.ShouldBeNull();
        created.Value.Level.ShouldBe(AssemblyLevel.Section);
    }

    [Fact]
    public void A_section_may_not_be_given_a_parent()
    {
        var created = AssemblyNode.Create(AssemblyLevel.Section, Parent, "S1", "Frame");

        created.IsFailure.ShouldBeTrue();
        created.Error.Code.ShouldBe("assembly.parent.not_allowed");
    }

    [Fact]
    public void Anything_below_a_section_must_have_a_parent()
    {
        foreach (var level in new[] { AssemblyLevel.Assembly, AssemblyLevel.SubAssembly })
        {
            var created = AssemblyNode.Create(level, null, "A1", "Gearbox");

            created.IsFailure.ShouldBeTrue($"{level} was created without a parent");
            created.Error.Code.ShouldBe("assembly.parent.required");
        }
    }

    [Fact]
    public void Create_normalises_the_code_and_trims_the_name()
    {
        var node = AssemblyNode.Create(AssemblyLevel.Section, null, "  s1  ", "  Frame  ").Value;

        // "s1", "S1 " and "S1" would otherwise be three different sections — the
        // duplicate-master problem that is cheap to prevent and expensive to unpick.
        node.Code.ShouldBe("S1");
        node.Name.ShouldBe("Frame");
    }

    [Fact]
    public void Create_upper_cases_the_manual_code_but_leaves_free_text_alone()
    {
        var node = AssemblyNode.Create(
            AssemblyLevel.Section,
            null,
            "S1",
            "Frame",
            new AssemblyNodeAttributes
            {
                ManualCode = " frm-1 ",
                // Upper-casing this would destroy the case of the unit symbols in it.
                TechnicalSpecification = "  Ø40 µm, 3 kΩ  ",
            }).Value;

        node.ManualCode.ShouldBe("FRM-1");
        node.TechnicalSpecification.ShouldBe("Ø40 µm, 3 kΩ");
    }

    [Fact]
    public void A_node_starts_active()
    {
        AssemblyNode.Create(AssemblyLevel.Section, null, "S1", "Frame").Value.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Update_can_move_an_assembly_to_another_section()
    {
        var node = AssemblyNode.Create(AssemblyLevel.Assembly, Parent, "A1", "Gearbox").Value;
        var newParent = AssemblyNodeId.New();

        var updated = node.Update(newParent, "Gearbox assembly", isActive: true);

        updated.IsSuccess.ShouldBeTrue();
        node.ParentId.ShouldBe(newParent);
        node.Name.ShouldBe("Gearbox assembly");
    }

    [Fact]
    public void Update_cannot_orphan_an_assembly()
    {
        var node = AssemblyNode.Create(AssemblyLevel.Assembly, Parent, "A1", "Gearbox").Value;

        var updated = node.Update(null, "Gearbox", isActive: true);

        updated.IsFailure.ShouldBeTrue();
        updated.Error.Code.ShouldBe("assembly.parent.required");
    }

    [Fact]
    public void Update_replaces_the_attributes_rather_than_merging_them()
    {
        var node = AssemblyNode.Create(
            AssemblyLevel.Section,
            null,
            "S1",
            "Frame",
            new AssemblyNodeAttributes { Remark = "Original", MachineType = "PRESS" }).Value;

        // A field left out of the payload is cleared. Silently keeping the old
        // value is how a remark nobody can delete comes about.
        node.Update(null, "Frame", isActive: true, new AssemblyNodeAttributes { MachineType = "PRESS" });

        node.Remark.ShouldBeNull();
        node.MachineType.ShouldBe("PRESS");
    }
}
