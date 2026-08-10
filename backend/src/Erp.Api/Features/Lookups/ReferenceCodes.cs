using Erp.Contracts.Masters;
using Erp.Api.Persistence;
using Erp.Api.Domain.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Lookups;

/// <summary>
/// Where a reference list actually lives, in one switch.
/// <para>
/// Most lists are rows in <c>LookupValue</c>. Two are not: units of measure and HSN
/// codes were promoted to masters of their own once they needed attributes a
/// four-column row cannot hold. Callers should not have to know which is which â€”
/// they ask for <c>uom</c> and get units, the same as they ask for <c>moc</c> and
/// get materials.
/// </para>
/// <para>
/// This exists so the promotion is recorded once. It is read by the endpoint that
/// serves dropdowns and by the check that rejects an unknown code, and if those two
/// disagreed about where units live, a form would offer an option the API refuses.
/// </para>
/// </summary>
public static class ReferenceCodes
{
    /// <summary>
    /// The options for a list that has its own table, or null for one that does not
    /// â€” meaning the caller should read it from <c>LookupValue</c>.
    /// </summary>
    public static IQueryable<LookupOptionDto>? OwnMaster(ErpDbContext db, string type)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (string.Equals(type, LookupTypes.UnitOfMeasure, StringComparison.OrdinalIgnoreCase))
        {
            return db.UnitsOfMeasure
                .AsNoTracking()
                .Where(unit => unit.IsActive)
                .OrderBy(unit => unit.SortOrder)
                .ThenBy(unit => unit.Name)
                .Select(unit => new LookupOptionDto { Code = unit.Code, Name = unit.Name });
        }

        if (string.Equals(type, LookupTypes.HsnCode, StringComparison.OrdinalIgnoreCase))
        {
            // By code, not by description: an HSN list is looked up by number, and
            // the numbers are grouped by chapter, which is an order that means
            // something to whoever is reading it.
            return db.HsnCodes
                .AsNoTracking()
                .Where(hsn => hsn.IsActive)
                .OrderBy(hsn => hsn.Code)
                .Select(hsn => new LookupOptionDto { Code = hsn.Code, Name = hsn.Description });
        }

        return null;
    }

    /// <summary>
    /// Every code currently valid for each of the given lists, keyed by list.
    /// <para>
    /// One query for the promoted masters and one for all the rest, not one per
    /// field â€” a part has twelve coded fields, and checking them a field at a time
    /// would be twelve round trips per saved record.
    /// </para>
    /// <para>
    /// Case-insensitive, and that is not tidiness. <c>Part</c> upper-cases some codes
    /// on the way in (<c>Moc</c>, <c>PartCategoryCode</c>) and leaves others as typed
    /// (<c>PartType</c>, <c>SourceCode</c>), while the stored options are mixed case
    /// â€” <c>"Mild steel"</c>. An ordinal comparison here would reject the very rows
    /// the master was seeded with.
    /// </para>
    /// </summary>
    public static async Task<Dictionary<string, HashSet<string>>> KnownAsync(
        ErpDbContext db,
        IReadOnlyList<string> types,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(types);

        var known = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var fromLookupValue = new List<string>();

        foreach (var type in types.Distinct(StringComparer.Ordinal))
        {
            var ownMaster = OwnMaster(db, type);

            if (ownMaster is null)
            {
                fromLookupValue.Add(type);
                continue;
            }

            var codes = await ownMaster
                .Select(option => option.Code)
                .ToListAsync(cancellationToken);

            known[type] = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (fromLookupValue.Count > 0)
        {
            var rows = await db.LookupValues
                .AsNoTracking()
                .Where(value => value.IsActive && fromLookupValue.Contains(value.Type))
                .Select(value => new { value.Type, value.Code })
                .ToListAsync(cancellationToken);

            foreach (var type in fromLookupValue)
            {
                known[type] = rows
                    .Where(row => string.Equals(row.Type, type, StringComparison.Ordinal))
                    .Select(row => row.Code)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }

        return known;
    }

    /// <summary>
    /// Whether a value is one of a list's codes. A blank value is accepted â€” an
    /// optional field left empty is not an unknown code, and requiredness is decided
    /// by the validator, not here.
    /// </summary>
    public static bool IsKnown(Dictionary<string, HashSet<string>> known, string lookupType, string? code)
    {
        ArgumentNullException.ThrowIfNull(known);

        return string.IsNullOrWhiteSpace(code)
            || (known.TryGetValue(lookupType, out var codes) && codes.Contains(code.Trim()));
    }
}
