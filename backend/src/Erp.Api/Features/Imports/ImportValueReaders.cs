using Erp.Api.Common.Excel;
using Erp.Api.Domain.Common;

namespace Erp.Api.Features.Imports;

/// <summary>Reads the approval status shared by the ported masters.</summary>
public static class MasterStatusReader
{
    /// <summary>
    /// Blank means <see cref="MasterStatus.Draft"/>.
    /// <para>
    /// A hand-typed sheet usually leaves the lifecycle columns out, and the sensible
    /// reading of an omitted status is "new record, not yet approved". Defaulting to
    /// Approved instead would let an import wave records past the approval step
    /// simply by not mentioning it.
    /// </para>
    /// </summary>
    public static MasterStatus Read(ImportRowReader reader, ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var text = reader.Text(column);

        if (string.IsNullOrWhiteSpace(text))
        {
            return MasterStatus.Draft;
        }

        if (Enum.TryParse<MasterStatus>(text, ignoreCase: true, out var status) && Enum.IsDefined(status))
        {
            return status;
        }

        reader.AddError(
            $"'{text}' is not a status. Use Draft, PendingApproval, Approved, Rejected or Hold.",
            column);

        return MasterStatus.Draft;
    }
}

/// <summary>Reads a GST percentage.</summary>
public static class TaxRate
{
    /// <summary>
    /// A rate, bounded to 0â€“100.
    /// <para>
    /// The bound is worth having because the commonest import mistake in this column
    /// is entering the tax <em>amount</em> rather than the rate. A stored 4,500%
    /// looks plausible in a grid cell and is only noticed when an invoice is wrong.
    /// </para>
    /// </summary>
    public static decimal? Read(ImportRowReader reader, ImportColumn column)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var value = reader.Number(column);

        if (value is < 0 or > 100)
        {
            reader.AddError("Must be a percentage between 0 and 100.", column);
            return null;
        }

        return value;
    }
}

/// <summary>Marks rows this system loaded from a spreadsheet.</summary>
public static class ImportProvenance
{
    /// <summary>
    /// Written to the legacy <c>ProgramId</c> column, which records which screen
    /// created a row. Keeping it filled in means a migrated master can still be
    /// asked "where did this record come from?" a year later â€” the question the
    /// column exists to answer, and one that import would otherwise leave blank.
    /// </summary>
    public const string ProgramId = "IMPORT";
}
