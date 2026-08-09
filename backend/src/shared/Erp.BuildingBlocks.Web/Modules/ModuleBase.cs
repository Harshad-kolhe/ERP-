using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.BuildingBlocks.Web.Modules;

/// <summary>
/// Default <see cref="IModule"/> implementation: builds the module's versioned
/// route group and maps every <see cref="IEndpoint"/> found in the module's own
/// assembly.
/// <para>
/// Adding an endpoint therefore means adding one file. There is no registration
/// list to update and no chance of writing an endpoint that is never mapped.
/// </para>
/// </summary>
public abstract class ModuleBase : IModule
{
    public abstract string Name { get; }

    /// <summary>Path segment under <c>/api/v1/</c>, e.g. <c>masters</c>.</summary>
    protected abstract string RoutePrefix { get; }

    public abstract void RegisterServices(IServiceCollection services, IConfiguration configuration);

    public virtual void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup($"/api/v1/{RoutePrefix}")
            .WithMetadata(new TagsAttribute(Name));

        foreach (var endpoint in DiscoverEndpoints())
        {
            endpoint.Map(group);
        }
    }

    private List<IEndpoint> DiscoverEndpoints()
    {
        // Only this module's assembly: a module can never map another module's endpoints.
        return GetType().Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
                && typeof(IEndpoint).IsAssignableFrom(t))
            .Select(t => (IEndpoint)Activator.CreateInstance(t)!)
            .ToList();
    }
}
