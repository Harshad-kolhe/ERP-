namespace Erp.Contracts.Auth;

public sealed record LoginRequest
{
    /// <summary>
    /// The sign-in identifier. Email rather than a separate username: one fewer
    /// thing for a user to remember, and <c>ErpUser</c> already requires it to be
    /// unique.
    /// </summary>
    public required string Email { get; init; }

    public required string Password { get; init; }
}
