using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Api.Common.Web;

public static class FeatureServiceRegistration
{
    private static readonly string[] Suffixes = ["Service", "Queries"];

    public static IServiceCollection AddFeatureServices(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var featureServices = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .Where(type => type.Namespace?.StartsWith("Erp.Api.Features", StringComparison.Ordinal) == true)
            .Where(type => Suffixes.Any(suffix => type.Name.EndsWith(suffix, StringComparison.Ordinal)));

        foreach (var type in featureServices)
        {
            services.AddScoped(type);
        }

        return services;
    }
}
