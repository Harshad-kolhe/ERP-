using System.Reflection;
using Erp.Contracts.Common;
using Erp.Api.Common.Results;

namespace Erp.ArchitectureTests;

/// <summary>
/// Keeps the inner layers free of the outer ones.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly string[] AllowedForPureLayers =
    [
        "System",
        "netstandard",
        "mscorlib",
    ];

    private static readonly string[] PureNamespaces =
    [
        "Erp.Api.Common.Results",
        "Erp.Api.Common.Entities",
        "Erp.Api.Common.Values",
        "Erp.Api.Common.Time",
    ];

    [Fact]
    public void Core_primitives_have_no_framework_dependencies()
    {
        var pureTypes = typeof(Result).Assembly
            .GetTypes()
            .Where(type => PureNamespaces.Any(ns =>
                type.Namespace?.StartsWith(ns, StringComparison.Ordinal) == true))
            .ToList();

        pureTypes.Count.ShouldBeGreaterThan(10,
            "guards the guard: if these namespaces move, the check below passes over an empty set.");

        var offenders = pureTypes
            .SelectMany(ReferencedTypes)
            .Select(referenced => referenced.Assembly.GetName().Name ?? string.Empty)
            .Where(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "Result, the entity primitives and the value objects are depended on by everything, so "
            + "their dependencies become everyone's. They must not see EF Core or ASP.NET Core. Found: "
            + string.Join(", ", offenders));
    }

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        if (type.BaseType is not null)
        {
            yield return type.BaseType;
        }

        foreach (var contract in type.GetInterfaces())
        {
            yield return contract;
        }

        foreach (var property in type.GetProperties(All))
        {
            yield return property.PropertyType;
        }

        foreach (var method in type.GetMethods(All))
        {
            yield return method.ReturnType;

            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
    }

    /// <summary>
    /// Contracts are the wire format: serialised, published in OpenAPI and generated
    /// into the TypeScript client. A dependency here is how an EF entity ends up
    /// being returned to a browser, which is what the legacy system did for most of
    /// its endpoints â€” it had 41 DTOs for 146 entities.
    /// </summary>
    [Fact]
    public void Contracts_have_no_dependencies()
    {
        var offenders = ExternalReferences(typeof(PagedResult<>).Assembly);

        offenders.ShouldBeEmpty(
            "Erp.Contracts must stay dependency-free. Found: " + string.Join(", ", offenders));
    }

    private static List<string> ExternalReferences(Assembly assembly) =>
        [.. assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !AllowedForPureLayers.Any(allowed =>
                name.Equals(allowed, StringComparison.Ordinal)
                || name.StartsWith(allowed + ".", StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
}
