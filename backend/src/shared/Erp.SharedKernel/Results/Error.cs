namespace Erp.SharedKernel.Results;

/// <summary>
/// Classifies a failure so the web layer can map it to an HTTP status code
/// without every handler knowing about HTTP.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
}

/// <summary>
/// A typed, addressable failure.
/// <para>
/// The system this replaces signalled failure with <c>AckMsg = "Error: " + ex.Message</c>
/// returned under HTTP 200, which made both monitoring and client-side handling
/// impossible. An <see cref="Error"/> carries a stable <see cref="Code"/> that
/// clients can branch on and a <see cref="Type"/> that determines the status code.
/// </para>
/// </summary>
/// <param name="Code">Stable, dot-separated identifier, e.g. <c>part.number.duplicate</c>.</param>
/// <param name="Description">Human-readable text safe to show a user. Never an exception message.</param>
/// <param name="Type">Determines the HTTP status the web layer will emit.</param>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>The absence of an error. Only ever attached to a successful result.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public override string ToString() => $"{Code}: {Description}";
}
