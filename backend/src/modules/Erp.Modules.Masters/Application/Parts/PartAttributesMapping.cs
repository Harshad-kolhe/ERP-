using Erp.Contracts.Masters;
using Erp.Persistence.Domain.Parts;

namespace Erp.Modules.Masters.Application.Parts;

/// <summary>
/// Translates the part's descriptive fields between the wire contract and the
/// domain. One place, used by create, update and the detail read, so the three
/// cannot disagree about which contract field feeds which domain field.
/// </summary>
internal static class PartAttributesMapping
{
    /// <summary>
    /// A null payload becomes an empty set rather than "leave the current values
    /// alone". Update replaces the descriptive fields wholesale — see the note on
    /// <see cref="UpdatePartRequest.Attributes"/> — and an empty set here is what
    /// makes omitting them mean "clear them" consistently in both operations.
    /// </summary>
    public static PartAttributes ToDomain(PartAttributesDto? dto) =>
        dto is null
            ? new PartAttributes()
            : new PartAttributes
            {
                ItemNumber = dto.ItemNumber,
                TechnicalSpecification = dto.TechnicalSpecification,
                Moc = dto.Moc,
                PartCategoryCode = dto.PartCategoryCode,
                PartType = dto.PartType,
                FormCategory = dto.FormCategory,
                PurchaseUomCode = dto.PurchaseUomCode,
                SellingUomCode = dto.SellingUomCode,
                MaterialType = dto.MaterialType,
                SeriesCode = dto.SeriesCode,
                PartRevisionNo = dto.PartRevisionNo,
                SourceCode = dto.SourceCode,
                WeightKg = dto.WeightKg,
                LeadTimeDays = dto.LeadTimeDays,
                MinimumStockLevel = dto.MinimumStockLevel,
                ReorderPoint = dto.ReorderPoint,
                RevisionRemark = dto.RevisionRemark,
                HoldRemark = dto.HoldRemark,
                InactiveRemark = dto.InactiveRemark,
            };
}
