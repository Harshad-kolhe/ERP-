namespace Erp.Contracts.Common;

/// <summary>
/// Stable <c>type</c> URIs for RFC 9457 problem responses.
/// <para>
/// The system this replaces returned <c>{ Status, AckMsg, MsgType, Data }</c> with
/// HTTP 200 even on failure, so no proxy, dashboard or client could tell a success
/// from an error. Here the status code carries the class of failure and this URI
/// identifies the specific kind, so clients branch on a value that is part of the
/// contract rather than on message text.
/// </para>
/// </summary>
public static class ProblemTypes
{
    private const string Prefix = "https://problems.erp/";

    /// <summary>Input failed validation. Accompanied by per-field errors. HTTP 400.</summary>
    public const string Validation = Prefix + "validation";

    /// <summary>The requested resource does not exist, or is invisible to this tenant. HTTP 404.</summary>
    public const string NotFound = Prefix + "not-found";

    /// <summary>A business rule rejected the operation, e.g. a duplicate part number. HTTP 409.</summary>
    public const string Conflict = Prefix + "conflict";

    /// <summary>The row changed since it was read. The client must reload and retry. HTTP 409.</summary>
    public const string ConcurrencyConflict = Prefix + "concurrency-conflict";

    /// <summary>No valid credentials were presented. HTTP 401.</summary>
    public const string Unauthorized = Prefix + "unauthorized";

    /// <summary>Authenticated, but lacking the permission this endpoint declares. HTTP 403.</summary>
    public const string Forbidden = Prefix + "forbidden";

    /// <summary>An unhandled fault. Carries a correlation id and never an exception message. HTTP 500.</summary>
    public const string Unexpected = Prefix + "unexpected";
}
