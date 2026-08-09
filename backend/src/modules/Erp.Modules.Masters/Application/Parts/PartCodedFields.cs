using Erp.BuildingBlocks.Excel;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Lookups;
using Erp.Modules.Masters.Application.Parts.ImportParts;
using Erp.Persistence;
using Erp.Persistence.Domain.Lookups;
using Erp.SharedKernel.Results;

namespace Erp.Modules.Masters.Application.Parts;

/// <summary>
/// The values a part carries that must already exist in a master, and where each
/// one comes from.
/// </summary>
/// <param name="LookupType">Which master answers for this field — see <see cref="LookupTypes"/>.</param>
/// <param name="Column">
/// The import column. Doubles as the field's label everywhere, because the sheet
/// headings are the grid captions, so an error names the field in the words the
/// operator is already looking at.
/// </param>
/// <param name="Read">Pulls this field's value out of a create or update payload.</param>
internal sealed record PartCodedField(
    string LookupType,
    ImportColumn Column,
    Func<PartCodes, string?> Read);

/// <summary>The three places a part's codes arrive from over HTTP.</summary>
internal sealed record PartCodes(string? UnitOfMeasureCode, string? HsnCode, PartAttributesDto? Attributes);

/// <summary>
/// One table, three readers.
/// <para>
/// Create, update and import all validate the same eleven fields against the same
/// masters, and each used to do it not at all. Listing them here rather than in
/// each handler is what stops the three drifting: adding a coded field to a part
/// means adding a line here, and all three paths start checking it.
/// </para>
/// <para>
/// <c>PartRevisionNo</c> and <c>SourceCode</c> are on this list and
/// <c>TechnicalSpecification</c> is not — the test is whether a master answers for
/// the value, not whether it is short.
/// </para>
/// </summary>
internal static class PartCodedFields
{
    public static readonly PartCodedField[] All =
    [
        new(LookupTypes.UnitOfMeasure, PartImportColumns.UnitOfMeasureCode, c => c.UnitOfMeasureCode),
        new(LookupTypes.HsnCode, PartImportColumns.HsnCode, c => c.HsnCode),
        new(LookupTypes.UnitOfMeasure, PartImportColumns.PurchaseUomCode, c => c.Attributes?.PurchaseUomCode),
        new(LookupTypes.UnitOfMeasure, PartImportColumns.SellingUomCode, c => c.Attributes?.SellingUomCode),
        new(LookupTypes.MaterialOfConstruction, PartImportColumns.Moc, c => c.Attributes?.Moc),
        new(LookupTypes.PartCategoryCode, PartImportColumns.PartCategoryCode, c => c.Attributes?.PartCategoryCode),
        new(LookupTypes.PartType, PartImportColumns.PartType, c => c.Attributes?.PartType),
        new(LookupTypes.PartFormCategory, PartImportColumns.FormCategory, c => c.Attributes?.FormCategory),
        new(LookupTypes.PartMaterialType, PartImportColumns.MaterialType, c => c.Attributes?.MaterialType),
        new(LookupTypes.PartSeriesCode, PartImportColumns.SeriesCode, c => c.Attributes?.SeriesCode),
        new(LookupTypes.PartRevisionNo, PartImportColumns.PartRevisionNo, c => c.Attributes?.PartRevisionNo),
        new(LookupTypes.PartSourceCode, PartImportColumns.SourceCode, c => c.Attributes?.SourceCode),
    ];

    /// <summary>The distinct masters these fields draw on — what a check has to load.</summary>
    public static readonly IReadOnlyList<string> LookupTypesUsed =
        [.. All.Select(field => field.LookupType).Distinct(StringComparer.Ordinal)];

    /// <summary>
    /// Checks a create or update payload's codes against the masters, returning the
    /// error to fail with or null if every code is known.
    /// <para>
    /// Every unknown code in one error rather than the first one found. A part
    /// submitted by an integration typically gets several fields wrong at once, and
    /// reporting them one save at a time is the round-trip-per-typo problem the
    /// import reader was built to avoid.
    /// </para>
    /// </summary>
    public static async Task<Error?> FindUnknownAsync(
        ErpDbContext db,
        PartCodes codes,
        CancellationToken cancellationToken)
    {
        var known = await ReferenceCodes.KnownAsync(db, LookupTypesUsed, cancellationToken);

        var unknown = All
            .Select(field => (field.Column.Header, Value: field.Read(codes), field.LookupType))
            .Where(entry => !ReferenceCodes.IsKnown(known, entry.LookupType, entry.Value))
            .Select(entry => $"'{entry.Value!.Trim()}' is not a known {entry.Header}.")
            .ToList();

        return unknown.Count == 0
            ? null
            : Error.Validation("part.code.unknown", string.Join(' ', unknown));
    }
}
