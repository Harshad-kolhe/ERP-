using System.Text.RegularExpressions;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Common;
using Erp.Api.Domain.Lookups;
using Erp.Api.Persistence;
using Erp.Contracts.Masters;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Lookups;

public sealed partial class LookupsService(ErpDbContext db)
{
    private const int MaxTypes = 25;

    public async Task<Result<LookupSetDto>> GetAsync(
        IReadOnlyList<string> types,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(types);

        var requested = types
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

            foreach (var type in requested.Where(type => !result.ContainsKey(type)))
            {
                result[type] = [];
            }
        }

        return Result.Success(new LookupSetDto { Lookups = result });
    }

    private static string Humanize(string name)
    {
        var words = SpaceBeforeCapital().Replace(name, " $1");

        return string.Concat(words[..1].ToUpperInvariant(), words[1..].ToLowerInvariant());
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex SpaceBeforeCapital();
}
