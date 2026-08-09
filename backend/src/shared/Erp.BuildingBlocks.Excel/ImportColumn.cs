namespace Erp.BuildingBlocks.Excel;

/// <summary>How a cell's text is interpreted, and how the template describes it.</summary>
public enum ImportColumnKind
{
    Text = 0,

    /// <summary>A whole number. Named to avoid colliding with the CLR type name (CA1720).</summary>
    WholeNumber = 1,

    /// <summary>A number that may have a fractional part.</summary>
    Number = 2,

    /// <summary>Accepts yes/no, true/false, y/n and 1/0, in any casing.</summary>
    Boolean = 3,

    /// <summary>A date. Real Excel dates are read as dates; text falls back to ISO <c>yyyy-MM-dd</c>.</summary>
    Date = 4,

    /// <summary>A comma-separated list in one cell, e.g. an employee's skills.</summary>
    TextList = 5,
}

/// <summary>
/// One column of an import sheet.
/// <para>
/// The point of declaring columns as data is that the same declaration drives both
/// halves of the feature: the template the operator downloads and the parser that
/// reads what they send back. A header can therefore never drift out of step with
/// the field it fills — which is the single most common way a hand-written importer
/// starts rejecting its own template.
/// </para>
/// </summary>
/// <param name="Header">Exactly as it appears in row 1. Matched case- and whitespace-insensitively on the way back in.</param>
/// <param name="Kind">How the cell is parsed.</param>
/// <param name="Required">Whether a blank cell is an error.</param>
/// <param name="MaxLength">Text ceiling, mirroring the database column so a rejection names the field.</param>
/// <param name="Note">Shown in the template's second sheet. Use for allowed values and formats.</param>
public sealed record ImportColumn(
    string Header,
    ImportColumnKind Kind = ImportColumnKind.Text,
    bool Required = false,
    int? MaxLength = null,
    string? Note = null);
