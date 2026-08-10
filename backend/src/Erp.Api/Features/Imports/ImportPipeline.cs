using Erp.Api.Common.Excel;
using Erp.Api.Common.Results;

namespace Erp.Api.Features.Imports;

/// <summary>
/// The steps every master import performs before it looks at a single value:
/// check the upload, open the sheet, confirm the required columns are there.
/// <para>
/// Shared so the six imports cannot answer "is this file acceptable?" six slightly
/// different ways. What is deliberately <em>not</em> shared is the mapping from row
/// to entity â€” that is the part that genuinely differs per master, and forcing it
/// through a common abstraction would produce a reflection-driven mapper nobody can
/// read or debug.
/// </para>
/// </summary>
public static class ImportPipeline
{
    /// <summary>Validates the upload and returns the parsed sheet, columns confirmed.</summary>
    public static Result<ExcelSheet> OpenSheet(
        Stream content,
        string? fileName,
        long length,
        IReadOnlyList<ImportColumn> columns)
    {
        var upload = ExcelUpload.Validate(fileName, length);

        if (upload.IsFailure)
        {
            return Result.Failure<ExcelSheet>(upload.Error);
        }

        var sheet = ExcelSheetReader.Read(content);

        if (sheet.IsFailure)
        {
            return sheet;
        }

        var required = ImportSheetBinder.RequireColumns(sheet.Value, columns);

        return required.IsFailure
            ? Result.Failure<ExcelSheet>(required.Error)
            : sheet;
    }

    /// <summary>
    /// Records a duplicate key found inside the uploaded file.
    /// <para>
    /// Checked separately from the database check because the two are different
    /// mistakes with different fixes: this one means the sheet contradicts itself,
    /// and no amount of looking at the existing data explains it.
    /// </para>
    /// </summary>
    public static void RejectDuplicatesWithinFile(
        ImportReport report,
        IEnumerable<(int Row, string? Key)> keys,
        string columnHeader)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(keys);

        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (row, key) in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (seen.TryGetValue(key, out var firstRow))
            {
                report.Add(row, $"'{key}' already appears on row {firstRow} of this file.", columnHeader);
                continue;
            }

            seen[key] = row;
        }
    }
}
