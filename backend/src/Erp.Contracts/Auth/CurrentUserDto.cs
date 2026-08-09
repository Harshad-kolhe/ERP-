namespace Erp.Contracts.Auth;

/// <summary>
/// The signed-in user, as the web app sees them.
/// <para>
/// <see cref="Permissions"/> is for deciding which menu items and buttons to draw.
/// It is not an authorization decision: the server re-checks every endpoint against
/// its declared permission regardless. That separation is the point — the legacy
/// system used the client-side list as the <em>only</em> check, so hiding a button
/// was the entire security model.
/// </para>
/// </summary>
public sealed record CurrentUserDto
{
    public required Guid UserId { get; init; }

    public required string UserName { get; init; }

    public required int BusinessUnitId { get; init; }

    public required bool CanAccessAllBusinessUnits { get; init; }

    public required IReadOnlyList<string> Permissions { get; init; }

    /// <summary>
    /// True when this user holds a super-administrator role.
    /// <para>
    /// <see cref="Permissions"/> already lists everything they can do, so the client
    /// never needs to branch on this to decide what to render. It is here so the
    /// interface can label the account honestly.
    /// </para>
    /// </summary>
    public required bool IsSuperAdministrator { get; init; }
}
