using Erp.Contracts.Masters;
using Erp.Api.Domain.Assemblies;

namespace Erp.Api.Features.Assemblies;

/// <summary>
/// Translation between the wire shape and the domain shape, in one place so create,
/// update and detail cannot disagree about what a field means.
/// </summary>
public static class AssemblyNodeMapping
{
    /// <summary>
    /// Contract attributes to domain attributes. A null payload becomes an empty
    /// set rather than "leave what is there": the update endpoint is a replace, and
    /// silently keeping old values is how a field nobody can clear comes about.
    /// </summary>
    public static AssemblyNodeAttributes ToDomain(AssemblyNodeAttributesDto? attributes) => new()
    {
        ManualCode = attributes?.ManualCode,
        MachineType = attributes?.MachineType,
        DrivenBy = attributes?.DrivenBy,
        DrawingPath = attributes?.DrawingPath,
        TechnicalSpecification = attributes?.TechnicalSpecification,
        Remark = attributes?.Remark,
        Quantity = attributes?.Quantity,
        WeightKg = attributes?.WeightKg,
        DisplaySequence = attributes?.DisplaySequence,
    };

    public static AssemblyNodeAttributesDto ToDto(AssemblyNode node) => new()
    {
        ManualCode = node.ManualCode,
        MachineType = node.MachineType,
        DrivenBy = node.DrivenBy,
        DrawingPath = node.DrawingPath,
        TechnicalSpecification = node.TechnicalSpecification,
        Remark = node.Remark,
        Quantity = node.Quantity,
        WeightKg = node.WeightKg,
        DisplaySequence = node.DisplaySequence,
    };

    /// <summary>
    /// The domain enum is <c>internal</c> and the contract enum is public; they are
    /// declared with matching members so the cast is total. Written as a switch
    /// rather than a numeric cast so adding a level to one and not the other is a
    /// compile error instead of a value that serialises as a number nobody expects.
    /// </summary>
    public static AssemblyLevelDto ToDto(AssemblyLevel level) => level switch
    {
        AssemblyLevel.Section => AssemblyLevelDto.Section,
        AssemblyLevel.Assembly => AssemblyLevelDto.Assembly,
        AssemblyLevel.SubAssembly => AssemblyLevelDto.SubAssembly,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unmapped assembly level."),
    };
}
