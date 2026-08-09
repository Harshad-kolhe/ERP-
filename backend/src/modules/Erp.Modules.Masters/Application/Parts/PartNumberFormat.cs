using System.Text.RegularExpressions;

namespace Erp.Modules.Masters.Application.Parts;

/// <summary>
/// What a part number and an HSN code are allowed to look like.
/// <para>
/// In one place because three callers need the same answer — the create validator,
/// the update validator and the importer — and a bulk import that accepted numbers
/// the single-record form rejects would let the master fill up with values no
/// screen can subsequently edit.
/// </para>
/// </summary>
internal static partial class PartNumberFormat
{
    public const int MaxPartNumberLength = 50;

    public const string PartNumberRule =
        "Part number may contain only letters, digits, dot, underscore, slash and hyphen.";

    public const string HsnCodeRule = "HSN code must be 4, 6 or 8 digits.";

    public static bool IsValidPartNumber(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= MaxPartNumberLength
        && PartNumberPattern().IsMatch(value.Trim());

    /// <summary>4, 6 or 8 digits, per the Indian GST schedule.</summary>
    public static bool IsValidHsnCode(string value) =>
        !string.IsNullOrWhiteSpace(value) && HsnCodePattern().IsMatch(value.Trim());

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._/-]*$")]
    private static partial Regex PartNumberPattern();

    [GeneratedRegex("^[0-9]{4}([0-9]{2}([0-9]{2})?)?$")]
    private static partial Regex HsnCodePattern();
}
