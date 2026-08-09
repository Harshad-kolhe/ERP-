using System.Globalization;
using Erp.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Erp.BuildingBlocks.Web.Security;

/// <summary>
/// Reads the current principal out of <see cref="HttpContext"/>.
/// <para>
/// The only place in the solution that knows claims exist. Handlers depend on
/// <see cref="ICurrentUser"/>, so they remain unit-testable with a stub and no web host.
/// </para>
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated =>
        accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public Guid UserId
    {
        get
        {
            var raw = accessor.HttpContext?.User.FindFirst(ErpClaimTypes.UserId)?.Value;

            // Deliberately throws rather than returning Guid.Empty. A missing claim
            // is a bug in sign-in, and the legacy system's version of this bug
            // attributed thousands of records to whichever user happened to be id 1.
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new InvalidOperationException(
                    "No authenticated user on this request. Check ICurrentUser.IsAuthenticated first.");
        }
    }

    public string UserName =>
        accessor.HttpContext?.User.FindFirst(ErpClaimTypes.UserName)?.Value ?? string.Empty;

    public IReadOnlySet<string> Permissions
    {
        get
        {
            var user = accessor.HttpContext?.User;

            if (user is null)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            return user.FindAll(ErpClaimTypes.Permission)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.Ordinal);
        }
    }

    public bool IsSuperAdministrator =>
        accessor.HttpContext?.User.HasClaim(c => c.Type == ErpClaimTypes.SuperAdministrator) == true;

    public bool HasPermission(string permission) =>
        accessor.HttpContext?.User.HasClaim(ErpClaimTypes.Permission, permission) == true;

    internal static int ReadBusinessUnit(IHttpContextAccessor accessor)
    {
        var raw = accessor.HttpContext?.User.FindFirst(ErpClaimTypes.BusinessUnit)?.Value;

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }
}
