using System.Globalization;
using Erp.Contracts.Import;
using Erp.SharedKernel.Results;

namespace Erp.BuildingBlocks.Excel;

/// <summary>
/// Failures that concern the file rather than any row in it.
/// <para>
/// All <see cref="ErrorType.Validation"/>: the upload was wrong, not the server, so
/// these are 400s the operator can act on. Problems with individual values are not
/// here — they are collected per row so the whole sheet can be reported at once.
/// </para>
/// </summary>
public static class ExcelErrors
{
    public static readonly Error Unreadable = Error.Validation(
        "import.file.unreadable",
        $"The file could not be opened. Save it as {ImportLimits.FileExtension} and upload it again.");

    public static readonly Error WrongExtension = Error.Validation(
        "import.file.extension",
        $"Only {ImportLimits.FileExtension} files are accepted. The older .xls format is not supported.");

    public static readonly Error Missing = Error.Validation(
        "import.file.missing",
        "No file was uploaded.");

    public static readonly Error TooLarge = Error.Validation(
        "import.file.too-large",
        $"The file is larger than {ImportLimits.MaxFileSizeBytes / (1024 * 1024)} MB.");

    public static readonly Error NoWorksheet = Error.Validation(
        "import.file.no-worksheet",
        "The workbook has no worksheets.");

    public static readonly Error Empty = Error.Validation(
        "import.file.empty",
        "The first worksheet is empty.");

    public static readonly Error NoHeaders = Error.Validation(
        "import.file.no-headers",
        "The first row must hold the column headings. Download the template to see them.");

    public static readonly Error NoDataRows = Error.Validation(
        "import.file.no-rows",
        "The sheet has headings but no data rows.");

    public static readonly Error TooManyRows = Error.Validation(
        "import.file.too-many-rows",
        $"An import is limited to {ImportLimits.MaxRows:N0} rows. Split the file and upload it in parts.");

    public static Error DuplicateHeader(string header) => Error.Validation(
        "import.file.duplicate-header",
        $"The heading '{header}' appears more than once. Each column must be unique.");

    public static Error MissingColumns(IEnumerable<string> headers) => Error.Validation(
        "import.file.missing-columns",
        "The sheet is missing required columns: "
        + string.Join(", ", headers)
        + ". Download the template and use its headings.");

    /// <summary>Rows failed. The detail is in the report body, so this only says how many.</summary>
    public static Error RowsRejected(int errorCount) => Error.Validation(
        "import.rows.rejected",
        string.Create(
            CultureInfo.InvariantCulture,
            $"The file was rejected: {errorCount} problem(s) found. Nothing was imported. Fix the sheet and upload it again."));
}
