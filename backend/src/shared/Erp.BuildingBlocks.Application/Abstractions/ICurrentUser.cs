namespace Erp.BuildingBlocks.Application.Abstractions;

/// <summary>
/// The authenticated principal, as the application layer sees it — without any
/// reference to <c>HttpContext</c>, so handlers stay testable.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The signed-in user's id.
    /// <para>
    /// Throws when unauthenticated rather than returning <see cref="Guid.Empty"/>.
    /// Three legacy modules read a claim that was never issued and silently stamped
    /// every row they created with user 1; failing loudly makes that impossible.
    /// </para>
    /// </summary>
    Guid UserId { get; }

    string UserName { get; }

    bool IsAuthenticated { get; }

    /// <summary>Permission codes granted through this user's roles.</summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// True when the user holds a super-administrator role.
    /// <para>
    /// Do not branch authorization on this. <see cref="Permissions"/> already
    /// contains everything the catalogue defines for such a user, so a check
    /// against a specific permission is correct on its own. This exists so the
    /// interface can say "all access" rather than enumerate, and so an audit
    /// record can note why the action was allowed.
    /// </para>
    /// </summary>
    bool IsSuperAdministrator { get; }

    bool HasPermission(string permission);
}
