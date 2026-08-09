using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Excel;
using Erp.Contracts.Import;
using Erp.Modules.Masters.Application.Imports;
using Erp.Persistence;
using Erp.Persistence.Domain.Parts;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.ImportParts;

internal sealed record ImportPartsCommand(ImportFile File);

/// <summary>
/// Loads a sheet of parts.
/// <para>
/// The shape of this handler is the same for every master, and is worth stating
/// once. Parse every row. Check every row against the file and against the
/// database. Only then, if nothing at all was wrong, write. There is no path
/// through this method that writes some rows and reports others as failed —
/// see <see cref="ImportResultDto"/> for why that matters more than it looks.
/// </para>
/// </summary>
internal sealed class ImportPartsHandler(ErpDbContext db)
    : ICommandHandler<ImportPartsCommand, ImportResultDto>
{
    public async Task<Result<ImportResultDto>> HandleAsync(
        ImportPartsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sheet = ImportPipeline.OpenSheet(
            command.File.Content,
            command.File.FileName,
            command.File.Length,
            PartImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("parts", rows.Count);
        var parts = new List<Part>(rows.Count);
        var keys = new List<(int Row, string? Key)>(rows.Count);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);

            // The key comes back from MapRow rather than being re-read here. Every
            // accessor records its own problems, so reading a column a second time
            // reports a blank required cell twice.
            var (part, key) = MapRow(reader);

            keys.Add((row.Row, key));
            report.Add(reader.Errors);

            if (part is not null)
            {
                parts.Add(part);
            }
        }

        ImportPipeline.RejectDuplicatesWithinFile(report, keys, PartImportColumns.PartNumber.Header);

        await RejectNumbersAlreadyInUse(report, keys, cancellationToken);

        if (report.HasErrors)
        {
            return Result.Success(report.Build(committed: false));
        }

        db.Parts.AddRange(parts);

        // One SaveChangesAsync for the whole file, so EF wraps it in a single
        // transaction. Nothing else is needed to make the import atomic.
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    /// <summary>
    /// Reads one row into a part and its business key.
    /// <para>
    /// The part is null when the row had any problem at all — the errors are
    /// already on the reader, and a half-built part would only be discarded. The key
    /// is returned even for a failed row, so the duplicate checks can still name it.
    /// </para>
    /// </summary>
    private static (Part? Part, string? Key) MapRow(ImportRowReader reader)
    {
        var partNumber = reader.RequiredText(PartImportColumns.PartNumber);

        // Upper-cased to match Part.Create's normalisation, so the duplicate checks
        // compare the value that will actually be stored.
        var key = string.IsNullOrEmpty(partNumber) ? null : partNumber.ToUpperInvariant();

        if (!string.IsNullOrEmpty(partNumber) && !PartNumberFormat.IsValidPartNumber(partNumber))
        {
            reader.AddError(PartNumberFormat.PartNumberRule, PartImportColumns.PartNumber);
        }

        var description = reader.RequiredText(PartImportColumns.Description);
        var unitOfMeasure = reader.RequiredText(PartImportColumns.UnitOfMeasureCode);
        var originalPartNumber = reader.Text(PartImportColumns.OriginalPartNumber);

        if (originalPartNumber is not null && !PartNumberFormat.IsValidPartNumber(originalPartNumber))
        {
            reader.AddError(PartNumberFormat.PartNumberRule, PartImportColumns.OriginalPartNumber);
        }

        var hsnCode = reader.Text(PartImportColumns.HsnCode);

        if (hsnCode is not null && !PartNumberFormat.IsValidHsnCode(hsnCode))
        {
            reader.AddError(PartNumberFormat.HsnCodeRule, PartImportColumns.HsnCode);
        }

        var weight = reader.Number(PartImportColumns.WeightKg);

        if (weight is < 0)
        {
            reader.AddError("Weight cannot be negative.", PartImportColumns.WeightKg);
        }

        var minimumStock = reader.Number(PartImportColumns.MinimumStockLevel);

        if (minimumStock is < 0)
        {
            reader.AddError("Minimum stock level cannot be negative.", PartImportColumns.MinimumStockLevel);
        }

        var leadTime = reader.WholeNumber(PartImportColumns.LeadTimeDays);

        if (leadTime is < 0)
        {
            reader.AddError("Lead time cannot be negative.", PartImportColumns.LeadTimeDays);
        }

        var reorderPoint = reader.WholeNumber(PartImportColumns.ReorderPoint);

        if (reorderPoint is < 0)
        {
            reader.AddError("Reorder point cannot be negative.", PartImportColumns.ReorderPoint);
        }

        // Blank means active and Draft. A sheet exported from the legacy system
        // fills both in; a sheet typed by hand usually leaves them out, and the
        // sensible reading of an omitted lifecycle is "a new, usable record".
        var isActive = reader.Boolean(PartImportColumns.IsActive) ?? true;
        var status = ReadStatus(reader);

        if (!reader.IsValid)
        {
            return (null, key);
        }

        var part = Part.Create(
            partNumber,
            description,
            categoryId: null,
            unitOfMeasure,
            hsnCode,
            reader.Text(PartImportColumns.DrawingNumber),
            new PartAttributes
            {
                ItemNumber = reader.Text(PartImportColumns.ItemNumber),
                TechnicalSpecification = reader.Text(PartImportColumns.TechnicalSpecification),
                Moc = reader.Text(PartImportColumns.Moc),
                PartCategoryCode = reader.Text(PartImportColumns.PartCategoryCode),
                PartType = reader.Text(PartImportColumns.PartType),
                FormCategory = reader.Text(PartImportColumns.FormCategory),
                PurchaseUomCode = reader.Text(PartImportColumns.PurchaseUomCode),
                SellingUomCode = reader.Text(PartImportColumns.SellingUomCode),
                MaterialType = reader.Text(PartImportColumns.MaterialType),
                SeriesCode = reader.Text(PartImportColumns.SeriesCode),
                PartRevisionNo = reader.Text(PartImportColumns.PartRevisionNo),
                SourceCode = reader.Text(PartImportColumns.SourceCode),
                WeightKg = weight,
                LeadTimeDays = leadTime,
                MinimumStockLevel = minimumStock,
                ReorderPoint = reorderPoint,
                RevisionRemark = reader.Text(PartImportColumns.RevisionRemark),
                HoldRemark = reader.Text(PartImportColumns.HoldRemark),
                InactiveRemark = reader.Text(PartImportColumns.InactiveRemark),
            },
            originalPartNumber);

        part.RestoreLifecycleState(status, isActive);

        // Re-checked: the attribute columns are read after the check above, and a
        // too-long value among them records an error there. Cheap, and it keeps the
        // "no part is built from a row with errors" invariant true.
        return (reader.IsValid ? part : null, key);
    }

    private static PartStatus ReadStatus(ImportRowReader reader)
    {
        var text = reader.Text(PartImportColumns.Status);

        if (string.IsNullOrWhiteSpace(text))
        {
            return PartStatus.Draft;
        }

        if (Enum.TryParse<PartStatus>(text, ignoreCase: true, out var status) && Enum.IsDefined(status))
        {
            return status;
        }

        reader.AddError(
            $"'{text}' is not a status. Use Draft, PendingApproval, Approved, Rejected or Hold.",
            PartImportColumns.Status);

        return PartStatus.Draft;
    }

    /// <summary>
    /// One query for the whole file, not one per row.
    /// <para>
    /// The unique index is still the guarantee — two operators can upload
    /// overlapping files at the same time and the loser's <c>SaveChanges</c> fails.
    /// This check exists so the ordinary case names the offending row instead of
    /// surfacing a constraint violation with no row number in it.
    /// </para>
    /// </summary>
    private async Task RejectNumbersAlreadyInUse(
        ImportReport report,
        List<(int Row, string? Key)> keys,
        CancellationToken cancellationToken)
    {
        var numbers = keys
            .Select(entry => entry.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (numbers.Count == 0)
        {
            return;
        }

        var existing = await db.Parts
            .AsNoTracking()
            .Where(part => numbers.Contains(part.PartNumber))
            .Select(part => part.PartNumber)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        var taken = existing.ToHashSet(StringComparer.Ordinal);

        foreach (var (row, key) in keys.Where(entry => entry.Key is not null && taken.Contains(entry.Key)))
        {
            report.Add(row, $"Part '{key}' already exists.", PartImportColumns.PartNumber.Header);
        }
    }
}
