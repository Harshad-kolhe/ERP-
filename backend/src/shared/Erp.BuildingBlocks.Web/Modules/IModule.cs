using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.BuildingBlocks.Web.Modules;

/// <summary>
/// A vertical slice of the system that owns its own domain, database schema,
/// endpoints and services.
/// <para>
/// Modules are discovered by assembly scan and register themselves. The legacy
/// <c>Program.cs</c> contained about 105 hand-written <c>AddScoped</c> lines, almost
/// all binding a concrete class to itself, which no one could safely delete from
/// because nothing indicated what was still used.
/// </para>
/// <para>
/// A module's types are <c>internal</c> apart from its <c>Integration/</c> folder,
/// so the compiler — not a naming convention — is what stops another module
/// reaching inside.
/// </para>
/// </summary>
public interface IModule
{
    /// <summary>Display name, used as the OpenAPI tag.</summary>
    string Name { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
