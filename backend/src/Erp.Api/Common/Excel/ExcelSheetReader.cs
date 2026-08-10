using System.Globalization;
using ClosedXML.Excel;
using Erp.Contracts.Import;
using Erp.Api.Common.Results;

namespace Erp.Api.Common.Excel;

/// <summary>
/// Turns an uploaded <c>.xlsx</c> into a header row and text cells.
/// <para>
/// It reads the first worksheet only. An import sheet with a second tab is almost
/// always a template's own instructions tab, and guessing which one holds the data
/// is how an operator silently imports the wrong thing.
/// </para>
/// </summary>
public static class ExcelSheetReader
{
    /// <summary>
    /// Reads the first worksheet.
    /// <para>
    /// Failures here are about the <em>file</em> â€” unreadable, empty, too large,
    /// duplicate headings. Anything about a particular value is a row error instead,
    /// reported through <see cref="ImportRowReader"/>, because the operator can fix
    /// those all at once.
    /// </para>
    /// </summary>
    public static Result<ExcelSheet> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // The exception message is not surfaced: it names internal parts of the
            // OOXML package and tells the operator nothing they can act on.
            return Result.Failure<ExcelSheet>(ExcelErrors.Unreadable);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                return Result.Failure<ExcelSheet>(ExcelErrors.NoWorksheet);
            }

            var used = worksheet.RangeUsed();

            if (used is null)
            {
                return Result.Failure<ExcelSheet>(ExcelErrors.Empty);
            }

            var firstRow = used.FirstRow().RowNumber();
            var lastRow = used.LastRow().RowNumber();
            var firstColumn = used.FirstColumn().ColumnNumber();
            var lastColumn = used.LastColumn().ColumnNumber();

            var headerResult = ReadHeaders(worksheet, firstRow, firstColumn, lastColumn);

            if (headerResult.IsFailure)
            {
                return Result.Failure<ExcelSheet>(headerResult.Error);
            }

            var headers = headerResult.Value;

            // Excel's used range routinely extends past the last real row â€” a cleared
            // cell still counts as used â€” so the cap is checked against rows that
            // actually carry data, counted below, not against this span.
            if (lastRow - firstRow > ImportLimits.MaxRows + 1000)
            {
                return Result.Failure<ExcelSheet>(ExcelErrors.TooManyRows);
            }

            var rows = new List<ExcelRow>();

            for (var rowNumber = firstRow + 1; rowNumber <= lastRow; rowNumber++)
            {
                var cells = ReadRow(worksheet, rowNumber, firstColumn, headers);

                // A row of nothing is a row somebody deleted the contents of. Skipping
                // it silently is right: rejecting the file for trailing blank rows
                // would fail almost every hand-edited sheet.
                if (cells.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                rows.Add(new ExcelRow(rowNumber, cells));

                if (rows.Count > ImportLimits.MaxRows)
                {
                    return Result.Failure<ExcelSheet>(ExcelErrors.TooManyRows);
                }
            }

            return rows.Count == 0
                ? Result.Failure<ExcelSheet>(ExcelErrors.NoDataRows)
                : Result.Success(new ExcelSheet(headers, rows));
        }
    }

    /// <summary>
    /// Collapses a heading to its comparable form: trimmed, inner whitespace
    /// squeezed to one space, lower-cased.
    /// <para>
    /// So "Part  Description", "part description" and "Part Description " are one
    /// column. Spreadsheets are edited by hand and those three are the same intent;
    /// treating them as different is a rejection the operator cannot see the cause of.
    /// </para>
    /// </summary>
    public static string NormalizeHeader(string header)
    {
        ArgumentNullException.ThrowIfNull(header);

        return string.Join(
            ' ',
            header.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToLowerInvariant();
    }

    private static Result<IReadOnlyList<string>> ReadHeaders(
        IXLWorksheet worksheet,
        int headerRow,
        int firstColumn,
        int lastColumn)
    {
        var headers = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            var text = worksheet.Cell(headerRow, column).GetFormattedString().Trim();

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            // Two columns that normalise the same would make one of them
            // unreachable, and which one won would depend on column order.
            if (!seen.Add(NormalizeHeader(text)))
            {
                return Result.Failure<IReadOnlyList<string>>(ExcelErrors.DuplicateHeader(text));
            }

            headers.Add(text);
        }

        return headers.Count == 0
            ? Result.Failure<IReadOnlyList<string>>(ExcelErrors.NoHeaders)
            : Result.Success<IReadOnlyList<string>>(headers);
    }

    private static Dictionary<string, string> ReadRow(
        IXLWorksheet worksheet,
        int rowNumber,
        int firstColumn,
        IReadOnlyList<string> headers)
    {
        var cells = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < headers.Count; index++)
        {
            var cell = worksheet.Cell(rowNumber, firstColumn + index);

            // Dates are pulled out as a round-trippable ISO string rather than
            // through the cell's display format, which follows whatever locale the
            // author's Excel used â€” that is how 03/04/2026 becomes two different days.
            var text = cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var date)
                ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : cell.GetFormattedString();

            cells[NormalizeHeader(headers[index])] = text.Trim();
        }

        return cells;
    }
}
