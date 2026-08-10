using Erp.Api.Common.Security;
using Microsoft.AspNetCore.Http;

namespace Erp.Api.Common.Security;

/// <summary>
/// Supplies the tenancy dimension to the EF global query filter.
/// <para>
/// Note what happens for an unauthenticated request: <see cref="BusinessUnitId"/>
/// returns 0, which matches no row. The filter therefore fails closed. Combined
/// with the fallback authorization policy, an endpoint that somehow escapes
/// authentication still returns nothing rather than everything.
/// </para>
/// </summary>
internal sealed class HttpContextBusinessUnitContext(IHttpContextAccessor accessor) : IBusinessUnitContext
{
    public int BusinessUnitId => HttpContextCurrentUser.ReadBusinessUnit(accessor);

    public bool CanAccessAllBusinessUnits =>
        accessor.HttpContext?.User.HasClaim(c => c.Type == ErpClaimTypes.AllBusinessUnits) == true;
}
