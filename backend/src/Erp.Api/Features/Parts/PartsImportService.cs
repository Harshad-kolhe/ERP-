using Erp.Api.Common.Excel;
using Erp.Api.Common.Results;
using Erp.Api.Domain.Parts;
using Erp.Api.Features.Imports;
using Erp.Api.Features.Lookups;
using Erp.Api.Persistence;
using Erp.Contracts.Import;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Features.Parts;

public sealed class PartsImportService(ErpDbContext db)
{
    public const string TemplateContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public const string TemplateSheetName = "Parts";

    public const string TemplateFileName = $"{TemplateSheetName}-import-template{ImportLimits.FileExtension}";

    public static byte[] BuildTemplate() =>
        ExcelTemplateWriter.Build(TemplateSheetName, PartImportColumns.All);

    public async Task<Result<ImportResultDto>> ImportAsync(
        ImportFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var sheet = ImportPipeline.OpenSheet(
            file.Content,
            file.FileName,
            file.Length,
            PartImportColumns.All);

        if (sheet.IsFailure)
        {
            return Result.Failure<ImportResultDto>(sheet.Error);
        }

        var rows = sheet.Value.Rows;
        var report = new ImportReport("parts", rows.Count);
        var parts = new List<Part>(rows.Count);
        var keys = new List<(int Row, string? Key)>(rows.Count);

        var known = await ReferenceCodes.KnownAsync(
            db,
            PartCodedFields.LookupTypesUsed,
            cancellationToken);

        foreach (var row in rows)
        {
            var reader = new ImportRowReader(row);

            var (part, key) = MapRow(reader, known);

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

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Build(committed: true));
    }

    private static (Part? Part, string? Key) MapRow(
        ImportRowReader reader,
        Dictionary<string, HashSet<string>> known)
    {
        var partNumber = reader.RequiredText(PartImportColumns.PartNumber);

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

        RejectUnknownCodes(reader, known);

        return (reader.IsValid ? part : null, key);
    }

    private static void RejectUnknownCodes(ImportRowReader reader, Dictionary<string, HashSet<string>> known)
    {
        foreach (var field in PartCodedFields.All)
        {
            var value = reader.Cell(field.Column);

            if (value is null || value.Length > field.Column.MaxLength)
            {
                continue;
            }

            if (!ReferenceCodes.IsKnown(known, field.LookupType, value))
            {
                reader.AddError($"'{value}' is not a known {field.Column.Header}.", field.Column);
            }
        }
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
