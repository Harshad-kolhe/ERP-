namespace Erp.BuildingBlocks.Excel;

/// <summary>
/// A worksheet reduced to a header row and text cells.
/// <para>
/// Everything is a string by the time it gets here. Typing happens later, in
/// <see cref="ImportRowReader"/>, where a failure can be reported against a named
/// column and row rather than thrown from inside a parser.
/// </para>
/// </summary>
/// <param name="Headers">Row 1, trimmed, in sheet order.</param>
/// <param name="Rows">Every row below it that is not entirely blank.</param>
public sealed record ExcelSheet(IReadOnlyList<string> Headers, IReadOnlyList<ExcelRow> Rows);

/// <summary>
/// One data row.
/// </summary>
/// <param name="Row">
/// The number Excel shows in its gutter, not a zero-based index. Errors quote this,
/// so the operator can open the file and go straight to the cell.
/// </param>
/// <param name="Cells">Keyed by normalised header — see <see cref="ExcelSheetReader.NormalizeHeader"/>.</param>
public sealed record ExcelRow(int Row, IReadOnlyDictionary<string, string> Cells);
