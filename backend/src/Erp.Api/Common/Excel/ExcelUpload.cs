using Erp.Contracts.Import;
using Erp.Api.Common.Results;

namespace Erp.Api.Common.Excel;

/// <summary>
/// The checks worth making before a byte of the upload is parsed.
/// <para>
/// Deliberately free of <c>IFormFile</c> so this stays out of the web layer: it
/// takes the name and the length, which is all the decision needs.
/// </para>
/// </summary>
public static class ExcelUpload
{
    /// <summary>
    /// Rejects a missing, oversized or wrong-typed upload.
    /// <para>
    /// The extension check is a courtesy, not a security control â€” it produces a
    /// clear message instead of the generic "could not be opened" that a <c>.xls</c>
    /// or a renamed PDF would otherwise get. The real defence is that the file is
    /// only ever handed to an OOXML reader, never executed or stored.
    /// </para>
    /// </summary>
    public static Result Validate(string? fileName, long length)
    {
        if (string.IsNullOrWhiteSpace(fileName) || length <= 0)
        {
            return Result.Failure(ExcelErrors.Missing);
        }

        if (length > ImportLimits.MaxFileSizeBytes)
        {
            return Result.Failure(ExcelErrors.TooLarge);
        }

        return Path.GetExtension(fileName).Equals(ImportLimits.FileExtension, StringComparison.OrdinalIgnoreCase)
            ? Result.Success()
            : Result.Failure(ExcelErrors.WrongExtension);
    }
}
