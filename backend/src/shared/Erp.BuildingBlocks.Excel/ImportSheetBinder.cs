using Erp.Contracts.Import;
using Erp.SharedKernel.Results;

namespace Erp.BuildingBlocks.Excel;

/// <summary>Checks an uploaded sheet against the columns a master declares.</summary>
public static class ImportSheetBinder
{
    /// <summary>
    /// Fails when a required column is absent from the sheet entirely.
    /// <para>
    /// Checked once, for the file, rather than per row. A required column that is
    /// missing produces one error the operator can act on; the per-row alternative
    /// produces the same message five thousand times and buries everything else.
    /// </para>
    /// </summary>
    public static Result RequireColumns(ExcelSheet sheet, IEnumerable<ImportColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(columns);

        var present = sheet.Headers
            .Select(ExcelSheetReader.NormalizeHeader)
            .ToHashSet(StringComparer.Ordinal);

        var missing = columns
            .Where(column => column.Required)
            .Where(column => !present.Contains(ExcelSheetReader.NormalizeHeader(column.Header)))
            .Select(column => column.Header)
            .ToList();

        return missing.Count == 0
            ? Result.Success()
            : Result.Failure(ExcelErrors.MissingColumns(missing));
    }
}

/// <summary>
/// Accumulates row errors and produces the final report.
/// <para>
/// Errors are capped at <see cref="ImportLimits.MaxReportedErrors"/>. Beyond that
/// the file is wrong in a structural way — a shifted column, the wrong template —
/// and the first two hundred already show it. Counting continues past the cap so
/// the operator is told the true scale.
/// </para>
/// </summary>
public sealed class ImportReport(string master, int totalRows)
{
    private readonly List<ImportRowErrorDto> _errors = [];

    private int _errorCount;

    public bool HasErrors => _errorCount > 0;

    public int ErrorCount => _errorCount;

    public void Add(IEnumerable<ImportRowErrorDto> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var error in errors)
        {
            _errorCount++;

            if (_errors.Count < ImportLimits.MaxReportedErrors)
            {
                _errors.Add(error);
            }
        }
    }

    public void Add(int row, string message, string? column = null) =>
        Add([new ImportRowErrorDto { Row = row, Column = column, Message = message }]);

    /// <summary>
    /// The report. <paramref name="committed"/> false means nothing was written —
    /// there is no third state, by design.
    /// </summary>
    public ImportResultDto Build(bool committed) => new()
    {
        Master = master,
        TotalRows = totalRows,
        ImportedRows = committed ? totalRows : 0,
        Committed = committed,
        // Sheet order, so reading the report top to bottom matches scrolling the file.
        Errors = [.. _errors.OrderBy(error => error.Row).ThenBy(error => error.Column, StringComparer.Ordinal)],
        ErrorsTruncated = _errorCount > _errors.Count,
    };
}
