using ClosedXML.Excel;

namespace Erp.BuildingBlocks.Excel;

/// <summary>
/// Writes the blank workbook an operator downloads before filling in an import.
/// <para>
/// Generated from the same <see cref="ImportColumn"/> list the parser reads, which
/// is the point: a template produced by hand drifts from the importer the first
/// time a field is added, and the operator discovers it as a rejection they cannot
/// explain.
/// </para>
/// </summary>
public static class ExcelTemplateWriter
{
    /// <summary>
    /// Builds a two-sheet workbook: the data sheet the importer reads, and a
    /// "Column guide" sheet describing every column.
    /// <para>
    /// The guide is a separate sheet rather than a comment row because the importer
    /// reads the first worksheet whole — an instructions row inside the data sheet
    /// would be imported as a part called "enter one row per part".
    /// </para>
    /// </summary>
    public static byte[] Build(string sheetName, IReadOnlyList<ImportColumn> columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        ArgumentNullException.ThrowIfNull(columns);

        using var workbook = new XLWorkbook();

        WriteHeaderSheet(workbook, sheetName, columns);
        WriteGuideSheet(workbook, columns);

        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer);

        return buffer.ToArray();
    }

    private static void WriteHeaderSheet(XLWorkbook workbook, string sheetName, IReadOnlyList<ImportColumn> columns)
    {
        var sheet = workbook.AddWorksheet(sheetName);

        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var cell = sheet.Cell(1, index + 1);

            cell.Value = column.Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xEF, 0xF2, 0xF7);

            // Required columns are marked in the sheet itself, not only in the guide.
            // Someone filling this in at speed is looking at the header row.
            if (column.Required)
            {
                cell.Style.Font.FontColor = XLColor.FromArgb(0xB0, 0x22, 0x22);
                cell.GetComment().AddText("Required");
            }

            // Text format on the whole column so codes keep their shape: an item code
            // of 00123 becomes 123 the moment Excel decides the column is numeric,
            // and a long numeric code turns into 1.23457E+14.
            if (column.Kind == ImportColumnKind.Text)
            {
                sheet.Column(index + 1).Style.NumberFormat.Format = "@";
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, 1);
    }

    private static void WriteGuideSheet(XLWorkbook workbook, IReadOnlyList<ImportColumn> columns)
    {
        var guide = workbook.AddWorksheet("Column guide");

        guide.Cell(1, 1).Value = "Column";
        guide.Cell(1, 2).Value = "Required";
        guide.Cell(1, 3).Value = "Type";
        guide.Cell(1, 4).Value = "Max length";
        guide.Cell(1, 5).Value = "Notes";
        guide.Row(1).Style.Font.Bold = true;

        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var row = index + 2;

            guide.Cell(row, 1).Value = column.Header;
            guide.Cell(row, 2).Value = column.Required ? "Yes" : "No";
            guide.Cell(row, 3).Value = Describe(column.Kind);
            guide.Cell(row, 4).Value = column.MaxLength?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
            guide.Cell(row, 5).Value = column.Note ?? "";
        }

        guide.SheetView.FreezeRows(1);
        guide.Columns().AdjustToContents(1, 1);
    }

    private static string Describe(ImportColumnKind kind) => kind switch
    {
        ImportColumnKind.WholeNumber => "Whole number",
        ImportColumnKind.Number => "Number",
        ImportColumnKind.Boolean => "Yes / No",
        ImportColumnKind.Date => "Date (yyyy-MM-dd)",
        ImportColumnKind.TextList => "Comma-separated list",
        _ => "Text",
    };
}
