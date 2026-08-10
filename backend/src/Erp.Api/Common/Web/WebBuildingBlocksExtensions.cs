using Erp.Api.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Api.Common.Web;

public static class WebBuildingBlocksExtensions
{
    /// <summary>
    /// Registers the request-scoped abstractions the application and persistence
    /// layers depend on, so neither has to know that HTTP exists.
    /// </summary>
    public static IServiceCollection AddErpWebBuildingBlocks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IBusinessUnitContext, HttpContextBusinessUnitContext>();

        return services;
    }
}
