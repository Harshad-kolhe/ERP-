using System.Globalization;
using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Excel;
using Erp.Contracts.Import;
using Erp.Api.Features.Imports;
using Erp.Api.Persistence;
using Erp.Api.Domain.BusinessUnits;
using Erp.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.BusinessUnits.ImportBusinessUnits;

public sealed record ImportBusinessUnitsCommand(ImportFile File);

/// <summary>
/// Loads a sheet of business units.
/// <para>
/// Two keys are checked rather than one. The name is unique system-wide, and the
/// unit id is what every other table's tenancy column holds â€” a duplicate there
/// would silently merge two tenants' data, which is the worst outcome this system
/// has, so it is checked as carefully as the name.
/// </para>
/// </summary>
public sealed class ImportBusinessUnitsHandler(ErpDbContext db)
    : ICommandHandler<ImportBusinessUnitsCommand, ImportResultDto>
{
    public async Task<Result<ImportResultDto>> HandleAsync(
        ImportBusinessUnitsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sheet = ImportPipeline.OpenSheet(
            command.File.Content,
            command.File.FileName,
            command.File.Length,
            BusinessUnitImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("business-units", rows.Count);
        var units = new List<BusinessUnit>(rows.Count);
        var names = new List<(int Row, string? Key)>(rows.Count);
        var ids = new List<(int Row, string? Key)>(rows.Count);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);
            var (unit, name, id) = MapRow(reader);

            names.Add((row.Row, name));
            ids.Add((row.Row, id));
            report.Add(reader.Errors);

            if (unit is not null)
            {
                units.Add(unit);
            }
        }

        ImportPipeline.RejectDuplicatesWithinFile(report, names, BusinessUnitImportColumns.BusinessName.Header);
        ImportPipeline.RejectDuplicatesWithinFile(report, ids, BusinessUnitImportColumns.BusinessUnitId.Header);

        await RejectExisting(report, names, ids, cancellationToken);

        if (report.HasErrors)
        {
            return Result.Success(report.Build(committed: false));
        }

        db.BusinessUnits.AddRange(units);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    private static (BusinessUnit? Unit, string? Name, string? Id) MapRow(ImportRowReader reader)
    {
        var unitId = reader.WholeNumber(BusinessUnitImportColumns.BusinessUnitId);

        if (unitId is < 1)
        {
            reader.AddError("Unit id must be a positive number.", BusinessUnitImportColumns.BusinessUnitId);
        }

        var name = reader.RequiredText(BusinessUnitImportColumns.BusinessName);

        var unit = new BusinessUnit
        {
            BusinessUnitId = unitId,
            BusinessName = name,
            Address = reader.Text(BusinessUnitImportColumns.Address),
            StateName = reader.Text(BusinessUnitImportColumns.StateName),
            StateCode = reader.Text(BusinessUnitImportColumns.StateCode),
            ContactNumber = reader.Text(BusinessUnitImportColumns.ContactNumber),
            Email = reader.Text(BusinessUnitImportColumns.Email),
            Website = reader.Text(BusinessUnitImportColumns.Website),
            Cin = reader.Text(BusinessUnitImportColumns.Cin),
            Gstn = reader.Text(BusinessUnitImportColumns.Gstn),
            Pan = reader.Text(BusinessUnitImportColumns.Pan),
            IsActive = reader.Boolean(BusinessUnitImportColumns.IsActive) ?? true,
        };

        return (
            reader.IsValid ? unit : null,
            string.IsNullOrEmpty(name) ? null : name,
            unitId?.ToString(CultureInfo.InvariantCulture));
    }

    private async Task RejectExisting(
        ImportReport report,
        List<(int Row, string? Key)> names,
        List<(int Row, string? Key)> ids,
        CancellationToken cancellationToken)
    {
        var wantedNames = names
            .Select(entry => entry.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var wantedIds = ids
            .Select(entry => entry.Key)
            .Where(key => key is not null)
            .Select(key => int.Parse(key!, CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();

        var existing = await db.BusinessUnits
            .AsNoTracking()
            .Where(unit =>
                (unit.BusinessName != null && wantedNames.Contains(unit.BusinessName))
                || (unit.BusinessUnitId != null && wantedIds.Contains(unit.BusinessUnitId.Value)))
            .Select(unit => new { unit.BusinessName, unit.BusinessUnitId })
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        var takenNames = existing
            .Select(unit => unit.BusinessName)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

        var takenIds = existing
            .Select(unit => unit.BusinessUnitId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        foreach (var (row, key) in names.Where(entry => entry.Key is not null && takenNames.Contains(entry.Key!)))
        {
            report.Add(row, $"A business unit named '{key}' already exists.", BusinessUnitImportColumns.BusinessName.Header);
        }

        foreach (var (row, key) in ids.Where(entry => entry.Key is not null))
        {
            if (takenIds.Contains(int.Parse(key!, CultureInfo.InvariantCulture)))
            {
                report.Add(row, $"Unit id '{key}' is already in use.", BusinessUnitImportColumns.BusinessUnitId.Header);
            }
        }
    }
}
