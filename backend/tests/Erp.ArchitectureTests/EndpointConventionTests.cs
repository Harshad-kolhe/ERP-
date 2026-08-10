using Erp.Api.Common.Security;
using Erp.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.ArchitectureTests;

/// <summary>
/// Rules every HTTP endpoint must satisfy, checked against the application's real
/// endpoint table.
/// </summary>
public sealed class EndpointConventionTests(ErpTestHost host) : IClassFixture<ErpTestHost>
{
    private IReadOnlyList<RouteEndpoint> Endpoints =>
        [.. host.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()];

    /// <summary>
    /// Replaces planned analyzer ERP0002.
    /// <para>
    /// The legacy system enforced permissions only in JavaScript and had zero
    /// server-side role or policy checks, so every restriction could be lifted with
    /// the browser console. Here, an endpoint must state its access rule out loud â€”
    /// a permission, an explicit authenticated-only marker, or explicit anonymity.
    /// Forgetting is not one of the options.
    /// </para>
    /// </summary>
    [Fact]
    public void Every_endpoint_declares_its_access_rule()
    {
        var undeclared = Endpoints
            .Where(endpoint =>
                endpoint.Metadata.GetMetadata<IPermissionDeclaration>() is null
                && endpoint.Metadata.GetMetadata<IAuthenticatedOnlyDeclaration>() is null
                && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Select(Describe)
            .ToList();

        undeclared.ShouldBeEmpty(
            "every endpoint must call RequirePermission(...), RequireAuthenticatedUserOnly(), "
            + "or AllowAnonymous(). Undeclared endpoints:\n" + string.Join('\n', undeclared));
    }

    /// <summary>
    /// Replaces planned analyzer ERP0005.
    /// <para>
    /// Of roughly 180 list grids in the legacy system, 12 were genuinely server-paged;
    /// about 149 shipped the entire result set to the browser. Requiring the paged
    /// contract on collection endpoints makes the cheap, wrong version unexpressible.
    /// </para>
    /// </summary>
    [Fact]
    public void Collection_endpoints_return_a_paged_contract()
    {
        var offenders = new List<string>();

        foreach (var endpoint in Endpoints)
        {
            if (!IsGet(endpoint) || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                continue;
            }

            var successTypes = endpoint.Metadata
                .OfType<IProducesResponseTypeMetadata>()
                .Where(metadata => metadata.StatusCode is >= 200 and < 300)
                .Select(metadata => metadata.Type)
                .Where(type => type is not null)
                .ToList();

            foreach (var type in successTypes)
            {
                if (IsBareCollection(type!))
                {
                    offenders.Add($"{Describe(endpoint)} returns {type!.Name}");
                }
            }
        }

        offenders.ShouldBeEmpty(
            "list endpoints must return PagedResult<T> or CursorPage<T>, never a bare collection:\n"
            + string.Join('\n', offenders));
    }

    /// <summary>
    /// Endpoint names become the operation ids in OpenAPI, which become the function
    /// names in the generated TypeScript client. A duplicate silently overwrites a
    /// client method.
    /// </summary>
    [Fact]
    public void Endpoint_names_are_unique()
    {
        var duplicates = Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .Where(name => !string.IsNullOrEmpty(name))
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key!)
            .ToList();

        duplicates.ShouldBeEmpty("duplicate endpoint names: " + string.Join(", ", duplicates));
    }

    /// <summary>
    /// Every master has a reachable list endpoint.
    /// <para>
    /// The other tests in this class quantify over whatever endpoints happen to
    /// exist, so a master whose endpoint was never mapped passes all of them by
    /// being absent. This is the one test that fails when something is missing
    /// rather than merely malformed â€” and it is the only way to confirm a route is
    /// live without a session, because an unauthenticated request to a route that
    /// does not exist is answered 401 by the authentication middleware, exactly
    /// like a route that does.
    /// </para>
    /// </summary>
    [Fact]
    public void Every_master_exposes_a_list_endpoint()
    {
        string[] expected =
        [
            "/api/v1/masters/parts",
            "/api/v1/masters/suppliers",
            "/api/v1/masters/customers",
            "/api/v1/masters/employees",
            "/api/v1/masters/roles",
            "/api/v1/masters/business-units",
            "/api/v1/masters/sections",
            "/api/v1/masters/assemblies",
            "/api/v1/masters/sub-assemblies",
            "/api/v1/masters/parent-parts",
            "/api/v1/masters/lookup-values",
            "/api/v1/masters/units-of-measure",
            "/api/v1/masters/hsn-codes",
        ];

        var mapped = Endpoints
            .Where(IsGet)
            .Select(Route)
            .ToHashSet(StringComparer.Ordinal);

        var missing = expected.Where(route => !mapped.Contains(route)).ToList();

        missing.ShouldBeEmpty(
            "these master list endpoints are not mapped:\n" + string.Join('\n', missing));
    }

    /// <summary>
    /// Guards the discovery mechanism itself. If module scanning silently stopped
    /// working, every other test in this class would pass over an empty set.
    /// </summary>
    [Fact]
    public void Modules_actually_mapped_their_endpoints()
    {
        var moduleEndpoints = Endpoints
            .Where(endpoint => Route(endpoint).StartsWith("/api/v1/masters", StringComparison.Ordinal))
            .ToList();

        moduleEndpoints.ShouldNotBeEmpty(
            "no master endpoints are mapped. Check the registration list in ErpEndpoints.MapMasters "
            + "and that app.MapControllers() is called.");
    }

    private static string Route(RouteEndpoint endpoint) =>
        "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');

    private static bool IsGet(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(HttpMethods.Get) == true;

    private static bool IsBareCollection(Type type)
    {
        if (type == typeof(string) || !type.IsGenericType)
        {
            return type.IsArray;
        }

        var definition = type.GetGenericTypeDefinition();

        if (definition == typeof(PagedResult<>) || definition == typeof(CursorPage<>))
        {
            return false;
        }

        return definition == typeof(List<>)
            || definition == typeof(IReadOnlyList<>)
            || definition == typeof(IEnumerable<>)
            || definition == typeof(ICollection<>)
            || definition == typeof(IList<>);
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
        return $"{string.Join('/', methods)} {Route(endpoint)}";
    }
}
