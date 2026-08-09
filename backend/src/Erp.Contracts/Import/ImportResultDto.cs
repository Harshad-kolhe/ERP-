namespace Erp.Contracts.Import;

/// <summary>
/// What happened to an uploaded spreadsheet.
/// <para>
/// An import either commits every row or none of them. That is the whole design:
/// a partial import leaves the operator with a sheet they cannot simply correct
/// and re-upload, because some rows are now duplicates and some are not, and
/// working out which is a manual reconciliation nobody has time for. Instead every
/// row is parsed and checked first; if anything at all is wrong, nothing is written
/// and <see cref="Errors"/> lists every problem in the file at once.
/// </para>
/// <para>
/// So the loop is: upload, read the errors, fix the sheet, upload the same file
/// again. It converges, and it never half-lands.
/// </para>
/// </summary>
public sealed record ImportResultDto
{
    /// <summary>Which master was imported, e.g. <c>parts</c>. Echoed so a stored report is self-describing.</summary>
    public required string Master { get; init; }

    /// <summary>Data rows found in the sheet, excluding the header.</summary>
    public required int TotalRows { get; init; }

    /// <summary>Rows written. Equal to <see cref="TotalRows"/> when <see cref="Committed"/>, otherwise zero.</summary>
    public required int ImportedRows { get; init; }

    /// <summary>False when the file was rejected. Nothing was written in that case.</summary>
    public required bool Committed { get; init; }

    /// <summary>
    /// Every problem found, in sheet order. Capped — see
    /// <c>ImportLimits.MaxReportedErrors</c> — because a report longer than a
    /// screen is not read, and the first hundred are enough to see the pattern.
    /// </summary>
    public required IReadOnlyList<ImportRowErrorDto> Errors { get; init; }

    /// <summary>True when <see cref="Errors"/> was truncated.</summary>
    public required bool ErrorsTruncated { get; init; }
}

/// <summary>One problem with one cell, addressed the way the operator sees it in Excel.</summary>
public sealed record ImportRowErrorDto
{
    /// <summary>
    /// The row number as Excel shows it in the left-hand gutter, so the operator can
    /// go straight to it. Row 1 is the header, so data starts at row 2.
    /// </summary>
    public required int Row { get; init; }

    /// <summary>The column heading. Null for a problem with the row as a whole.</summary>
    public string? Column { get; init; }

    public required string Message { get; init; }
}
