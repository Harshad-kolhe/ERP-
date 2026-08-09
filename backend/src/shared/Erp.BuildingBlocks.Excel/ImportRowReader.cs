using System.Globalization;
using Erp.Contracts.Import;

namespace Erp.BuildingBlocks.Excel;

/// <summary>
/// Reads one row's cells as typed values, collecting problems instead of throwing.
/// <para>
/// That is the entire reason this exists. A parser that throws on the first bad
/// cell makes the operator fix one thing, re-upload, and discover the next — a
/// hundred round trips for a hundred typos. Every accessor here records what was
/// wrong and carries on, so a single upload reports everything wrong with the row.
/// </para>
/// <para>
/// A column absent from the sheet reads as blank rather than as an error, so an
/// operator may upload only the columns they care about. Columns that must be
/// present are checked once, for the file as a whole, by
/// <see cref="ImportSheetBinder.RequireColumns"/> — a missing required column is a
/// fact about the file, not about row 47.
/// </para>
/// </summary>
public sealed class ImportRowReader(ExcelRow row)
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd-MM-yyyy",
        "dd/MM/yyyy",
        "dd-MMM-yyyy",
        "yyyy-MM-ddTHH:mm:ss",
    ];

    private static readonly string[] TrueValues = ["true", "yes", "y", "1"];

    private static readonly string[] FalseValues = ["false", "no", "n", "0"];

    private readonly List<ImportRowErrorDto> _errors = [];

    /// <summary>The row number as Excel shows it.</summary>
    public int Row => row.Row;

    public IReadOnlyList<ImportRowErrorDto> Errors => _errors;

    public bool IsValid => _errors.Count == 0;

    /// <summary>Records a problem this class could not have found, e.g. a duplicate against the database.</summary>
    public void AddError(string message, ImportColumn? column = null) =>
        _errors.Add(new ImportRowErrorDto
        {
            Row = row.Row,
            Column = column?.Header,
            Message = message,
        });

    /// <summary>Trimmed text, or null when blank. Records an error if the column is required or over length.</summary>
    public string? Text(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        if (string.IsNullOrWhiteSpace(raw))
        {
            RequireIfNeeded(column);
            return null;
        }

        var value = raw.Trim();

        if (column.MaxLength is { } maximum && value.Length > maximum)
        {
            AddError($"Must be {maximum} characters or fewer; this is {value.Length}.", column);
            return null;
        }

        return value;
    }

    /// <summary>
    /// Text for a column the caller has already established is required.
    /// Returns empty rather than null when missing — the error is already recorded,
    /// and the row will be discarded, so the value is never used.
    /// </summary>
    public string RequiredText(ImportColumn column) => Text(column) ?? string.Empty;

    /// <summary>
    /// The cell as typed, recording nothing.
    /// <para>
    /// For a caller that needs to look at a column a <em>second</em> time — checking
    /// a code against a master, say, after <see cref="Text"/> already read it into an
    /// entity. Going through <see cref="Text"/> again would record a blank-required
    /// or over-length error twice for one cell, which is the duplicate-reporting
    /// problem this class exists to avoid.
    /// </para>
    /// </summary>
    public string? Cell(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }

    /// <summary>A number, possibly fractional. Named <c>Number</c> rather than <c>Decimal</c> per CA1720.</summary>
    public decimal? Number(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        if (string.IsNullOrWhiteSpace(raw))
        {
            RequireIfNeeded(column);
            return null;
        }

        // Invariant culture, explicitly. The cell arrives as whatever text Excel
        // displayed, and parsing that under the server's culture is how a decimal
        // point becomes a thousands separator on a machine configured differently
        // from the one that wrote the file.
        if (!decimal.TryParse(Clean(raw), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            AddError($"'{raw}' is not a number.", column);
            return null;
        }

        return value;
    }

    /// <summary>
    /// A whole number. Accepts <c>12.00</c> as 12, because a cell formatted with
    /// two decimals is a formatting choice rather than a different value; anything
    /// with a real fraction is rejected.
    /// </summary>
    public int? WholeNumber(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        if (string.IsNullOrWhiteSpace(raw))
        {
            RequireIfNeeded(column);
            return null;
        }

        if (!decimal.TryParse(Clean(raw), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            AddError($"'{raw}' is not a number.", column);
            return null;
        }

        if (value != decimal.Truncate(value))
        {
            AddError($"'{raw}' must be a whole number.", column);
            return null;
        }

        if (value is < int.MinValue or > int.MaxValue)
        {
            AddError($"'{raw}' is out of range.", column);
            return null;
        }

        return (int)value;
    }

    public bool? Boolean(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        if (string.IsNullOrWhiteSpace(raw))
        {
            RequireIfNeeded(column);
            return null;
        }

        var value = raw.Trim().ToLowerInvariant();

        if (TrueValues.Contains(value, StringComparer.Ordinal))
        {
            return true;
        }

        if (FalseValues.Contains(value, StringComparer.Ordinal))
        {
            return false;
        }

        AddError($"'{raw}' is not a yes/no value. Use Yes or No.", column);
        return null;
    }

    /// <summary>
    /// A date, stored at midnight UTC.
    /// <para>
    /// Real Excel date cells arrive as ISO text from <see cref="ExcelSheetReader"/>;
    /// the other formats accepted here are for dates somebody typed as text. Note
    /// that <c>dd/MM/yyyy</c> is read day-first, matching where this system is used
    /// — a US-style sheet would need its dates as real dates or ISO text.
    /// </para>
    /// </summary>
    public DateTimeOffset? Date(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        if (string.IsNullOrWhiteSpace(raw))
        {
            RequireIfNeeded(column);
            return null;
        }

        if (!DateTime.TryParseExact(
                raw.Trim(),
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            AddError($"'{raw}' is not a date. Use a date cell, or text in yyyy-MM-dd form.", column);
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
    }

    /// <summary>A comma-separated cell, split and trimmed. Blanks inside are dropped.</summary>
    public List<string> TextList(ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var raw = Raw(column);

        if (string.IsNullOrWhiteSpace(raw))
        {
            RequireIfNeeded(column);
            return [];
        }

        return [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    /// <summary>Absent column and blank cell are the same thing here — see the class remarks.</summary>
    private string? Raw(ImportColumn column) =>
        row.Cells.TryGetValue(ExcelSheetReader.NormalizeHeader(column.Header), out var value) ? value : null;

    private void RequireIfNeeded(ImportColumn column)
    {
        if (column.Required)
        {
            AddError("This column is required and is blank.", column);
        }
    }

    /// <summary>
    /// Strips the decorations a spreadsheet adds to a number it was told to display
    /// as currency or a percentage — the value is still the number underneath.
    /// </summary>
    private static string Clean(string raw) =>
        raw.Replace("₹", string.Empty, StringComparison.Ordinal)
            .Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
}
