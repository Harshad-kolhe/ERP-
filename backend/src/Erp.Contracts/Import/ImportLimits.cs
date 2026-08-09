namespace Erp.Contracts.Import;

/// <summary>
/// The bounds an import is held to. In the contract assembly because the client
/// shows them to the operator before they upload a file that would be rejected.
/// </summary>
public static class ImportLimits
{
    /// <summary>
    /// Data rows accepted in one file.
    /// <para>
    /// A whole-file transaction is what makes all-or-nothing possible, and a
    /// transaction holds locks for its duration — so this is the number that keeps
    /// an import from blocking the masters tables for minutes. A larger migration is
    /// several files, which is also easier to check.
    /// </para>
    /// </summary>
    public const int MaxRows = 5_000;

    /// <summary>Upload size ceiling, before the file is even opened.</summary>
    public const long MaxFileSizeBytes = 16L * 1024 * 1024;

    /// <summary>Errors returned before the list is truncated.</summary>
    public const int MaxReportedErrors = 200;

    /// <summary>The only accepted extension. The legacy <c>.xls</c> binary format is not read.</summary>
    public const string FileExtension = ".xlsx";
}
