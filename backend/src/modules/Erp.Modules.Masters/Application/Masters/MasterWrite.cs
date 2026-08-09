using Erp.SharedKernel.Results;

namespace Erp.Modules.Masters.Application.Masters;

/// <summary>
/// The pieces every master's create and update slice needs, in one place rather
/// than copied five times: decoding a row version, and the three failures they all
/// produce.
/// </summary>
internal static class MasterWrite
{
    /// <summary>
    /// Decodes the base64 <c>rowversion</c> the client echoes back.
    /// <para>
    /// Returns false rather than throwing on malformed input: a client sending
    /// nonsense here is a stale-data problem from the user's point of view, and
    /// "reload and try again" is the same answer either way.
    /// </para>
    /// </summary>
    public static bool TryDecodeRowVersion(string value, out byte[] rowVersion)
    {
        rowVersion = [];

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var buffer = new byte[((value.Length * 3) + 3) / 4];

        if (!Convert.TryFromBase64String(value, buffer, out var written))
        {
            return false;
        }

        rowVersion = buffer[..written];
        return true;
    }
}

/// <summary>Failures shared by the master write slices.</summary>
internal static class MasterErrors
{
    /// <summary>
    /// A record in another business unit is filtered out by the tenancy filter, so
    /// it is indistinguishable from one that does not exist. That is deliberate:
    /// 404 rather than 403 avoids confirming that the id is real.
    /// </summary>
    public static Error NotFound(string master, object id) => Error.NotFound(
        $"{master}.not-found",
        $"No {master} was found with id {id}.");

    public static Error DuplicateCode(string master, string field, string value) => Error.Conflict(
        $"{master}.{field}.duplicate",
        $"A {master} with {field} '{value}' already exists.");

    public static Error StaleRowVersion(string master) => Error.Conflict(
        $"{master}.stale",
        "This record changed since you opened it. Reload and re-apply your changes.");
}

/// <summary>
/// Trimming and casing, applied identically wherever a master record is written.
/// <para>
/// Codes are upper-cased so <c>"acme"</c>, <c>"ACME "</c> and <c>"Acme"</c> cannot
/// become three suppliers — the duplicate-master problem that is cheap to prevent
/// here and expensive to unpick later. Free text is only trimmed: upper-casing a
/// company name or an address would be vandalism.
/// </para>
/// </summary>
internal static class Normalize
{
    /// <summary>Trimmed text, with blank collapsed to null so "" and null are not two states.</summary>
    public static string? Text(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>As <see cref="Text"/>, then upper-cased. For codes, never for names.</summary>
    public static string? Code(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    /// <summary>A required code: trimmed and upper-cased, never null.</summary>
    public static string RequiredCode(string value) => value.Trim().ToUpperInvariant();
}
