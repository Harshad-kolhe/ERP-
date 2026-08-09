using System.Reflection;
using Erp.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.BuildingBlocks.Web.Modules;

/// <summary>
/// Discovers and wires modules by assembly scan.
/// </summary>
public static class ModuleRegistration
{
    private const string ModuleAssemblyPrefix = "Erp.Modules.";

    /// <summary>
    /// Finds every <see cref="IModule"/> in the referenced module assemblies and
    /// lets each register its own services.
    /// </summary>
    /// <param name="hostAssembly">The API assembly. Its project references define which modules exist.</param>
    /// <returns>
    /// The discovered modules, to be passed to <see cref="MapErpModules"/>. Returned
    /// rather than resolved from the container later so the composition root never
    /// needs to reach into the service provider.
    /// </returns>
    public static IReadOnlyList<IModule> AddErpModules(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly hostAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hostAssembly);

        var moduleAssemblies = ModuleAssemblies(hostAssembly);
        var modules = Discover(moduleAssemblies);

        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
        }

        RegisterPermissionCatalogue(services, moduleAssemblies);

        return modules;
    }

    /// <summary>
    /// Collects every module's <see cref="IPermissionSource"/> into one catalogue.
    /// <para>
    /// Scanned rather than listed, so a new module's permissions reach the roles
    /// screen by existing. The catalogue says what <em>can</em> be granted; the
    /// database says what is granted and to whom.
    /// </para>
    /// </summary>
    private static void RegisterPermissionCatalogue(
        IServiceCollection services,
        IReadOnlyList<Assembly> moduleAssemblies)
    {
        var sourceTypes = moduleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
                && typeof(IPermissionSource).IsAssignableFrom(type));

        foreach (var sourceType in sourceTypes)
        {
            services.AddSingleton(typeof(IPermissionSource), sourceType);
        }

        services.AddSingleton<IPermissionCatalogue, PermissionCatalogue>();
    }

    /// <summary>Maps every module's endpoints.</summary>
    public static IEndpointRouteBuilder MapErpModules(
        this IEndpointRouteBuilder endpoints,
        IReadOnlyList<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(modules);

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    private static IReadOnlyList<Assembly> ModuleAssemblies(Assembly hostAssembly) =>
    [
        .. ProbeOutputDirectory(hostAssembly)
            .Concat(ProbeManifest(hostAssembly))
            .DistinctBy(assembly => assembly.FullName, StringComparer.Ordinal),
    ];

    private static IReadOnlyList<IModule> Discover(IReadOnlyList<Assembly> moduleAssemblies)
    {
        return [.. moduleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
                && typeof(IModule).IsAssignableFrom(type))
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .OrderBy(module => module.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Primary discovery: the deployed output directory.
    /// <para>
    /// The manifest alone is not enough. A module is referenced by the host only as
    /// a project reference — the host never names one of its types in code, which is
    /// the whole point of the design — so the C# compiler elides the reference from
    /// the assembly manifest as unused. Relying on <see cref="Assembly.GetReferencedAssemblies"/>
    /// therefore discovered nothing and produced an API with no endpoints at all;
    /// the architecture tests caught it before it reached anyone.
    /// </para>
    /// </summary>
    private static IEnumerable<Assembly> ProbeOutputDirectory(Assembly hostAssembly)
    {
        var location = hostAssembly.Location;

        // Empty under single-file publishing, where ProbeManifest is the fallback.
        if (string.IsNullOrEmpty(location))
        {
            return [];
        }

        var directory = Path.GetDirectoryName(location);

        if (string.IsNullOrEmpty(directory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(directory, ModuleAssemblyPrefix + "*.dll")
            .Select(AssemblyName.GetAssemblyName)

            // Load by name so the default load context resolves it, rather than
            // LoadFrom, which can produce a second copy of an already-loaded assembly.
            .Select(Assembly.Load);
    }

    private static IEnumerable<Assembly> ProbeManifest(Assembly hostAssembly) =>
        hostAssembly
            .GetReferencedAssemblies()
            .Where(name => name.Name?.StartsWith(ModuleAssemblyPrefix, StringComparison.Ordinal) == true)
            .Select(Assembly.Load);
}
