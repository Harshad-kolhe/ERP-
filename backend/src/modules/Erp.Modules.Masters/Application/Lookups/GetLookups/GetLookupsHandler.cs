using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Contracts.Masters;
using Erp.Persistence;
using Erp.Persistence.Domain.Common;
using Erp.Persistence.Domain.Lookups;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Lookups.GetLookups;

internal sealed record GetLookupsQuery(IReadOnlyList<string> Types);

/// <summary>
/// Returns the option lists a form asked for.
/// <para>
/// Everything selectable in this module comes from here, which is what lets the web
/// app hold no list of choices at all. Adding a supplier type is a row in
/// <c>LookupValue</c>, not a deployment.
/// </para>
/// </summary>
internal sealed class GetLookupsHandler(ErpDbContext db)
    : IQueryHandler<GetLookupsQuery, LookupSetDto>
{
    /// <summary>
    /// A form asks for six or seven lists; anything much beyond that is a client
    /// asking for the whole table, which is not what this endpoint is for.
    /// </summary>
    private const int MaxTypes = 25;

    public async Task<Result<LookupSetDto>> HandleAsync(
        GetLookupsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requested = query.Types
            .Select(type => type.Trim())
            .Where(type => type.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTypes)
            .ToList();

        if (requested.Count == 0)
        {
            return Result.Failure<LookupSetDto>(Error.Validation(
                "lookup.types.missing",
                "Ask for at least one list, e.g. ?types=uom,currency."));
        }

        var result = new Dictionary<string, IReadOnlyList<LookupOptionDto>>(StringComparer.Ordinal);

        // The status list is derived, not stored — see LookupTypes.MasterStatus.
        // Handled first so the database query only asks for the rest.
        var statusRequested = requested.RemoveAll(
            type => string.Equals(type, LookupTypes.MasterStatus, StringComparison.OrdinalIgnoreCase)) > 0;

        if (statusRequested)
        {
            result[LookupTypes.MasterStatus] =
            [
                .. Enum.GetValues<MasterStatus>()
                    .Select(status => new LookupOptionDto
                    {
                        Code = status.ToString(),
                        Name = Humanize(status.ToString()),
                    }),
            ];
        }

        // Units of measure and HSN codes answer under their old list names but come
        // from their own tables — see ReferenceCodes. Handled here, next to the
        // status list, so a client asking for 'uom' is unaware anything moved.
        foreach (var type in requested.ToList())
        {
            var ownMaster = ReferenceCodes.OwnMaster(db, type);

            if (ownMaster is null)
            {
                continue;
            }

            result[type] = await ownMaster.ToListAsync(cancellationToken);
            requested.Remove(type);
        }

        if (requested.Count > 0)
        {
            var rows = await db.LookupValues
                .AsNoTracking()
                .Where(value => value.IsActive && requested.Contains(value.Type))
                .OrderBy(value => value.Type)
                .ThenBy(value => value.SortOrder)
                .ThenBy(value => value.Name)
                .Select(value => new { value.Type, value.Code, value.Name })
                .ToListAsync(cancellationToken);

            foreach (var group in rows.GroupBy(row => row.Type, StringComparer.Ordinal))
            {
                result[group.Key] =
                    [.. group.Select(row => new LookupOptionDto { Code = row.Code, Name = row.Name })];
            }

            // A list with no rows comes back empty rather than absent, so the client
            // renders an empty dropdown instead of failing on a missing key.
            foreach (var type in requested.Where(type => !result.ContainsKey(type)))
            {
                result[type] = [];
            }
        }

        return Result.Success(new LookupSetDto { Lookups = result });
    }

    /// <summary>Splits a PascalCase enum name into words: PendingApproval becomes "Pending approval".</summary>
    private static string Humanize(string name)
    {
        var words = System.Text.RegularExpressions.Regex.Replace(
            name,
            "(?<!^)([A-Z])",
            " $1",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromMilliseconds(100));

        return string.Concat(words[..1].ToUpperInvariant(), words[1..].ToLowerInvariant());
    }
}
