using Microsoft.AspNetCore.Identity;

namespace Erp.Api.Domain.Identity;

/// <summary>
/// An application user.
/// <para>
/// Backed by ASP.NET Core Identity, so passwords are salted and hashed with
/// PBKDF2 and never stored or compared in plain text â€” which the legacy system
/// did with a direct string comparison, alongside a second endpoint that accepted
/// a user's email address as their password.
/// </para>
/// <para>
/// This lives in the host for the Phase 0 vertical proof. It moves to
/// <c>Erp.Modules.Identity</c> in Phase 1 along with roles, permissions and session
/// presence.
/// </para>
/// </summary>
public sealed class ErpUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The business unit this user's data is scoped to.</summary>
    public int BusinessUnitId { get; set; }

    /// <summary>Set for principals allowed to read across every business unit.</summary>
    public bool CanAccessAllBusinessUnits { get; set; }
}
