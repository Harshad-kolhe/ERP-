using System.Reflection;
using Erp.Contracts.Common;
using Erp.SharedKernel.Results;

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

    /// <summary>
    /// The shared kernel holds <c>Result</c>, the entity primitives and the value
    /// objects. Everything depends on it, so it must depend on nothing — otherwise
    /// its dependencies become everyone's dependencies.
    /// </summary>
    [Fact]
    public void SharedKernel_has_no_dependencies()
    {
        var offenders = ExternalReferences(typeof(Result).Assembly);

        offenders.ShouldBeEmpty(
            "Erp.SharedKernel must stay dependency-free. Found: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// Contracts are the wire format: serialised, published in OpenAPI and generated
    /// into the TypeScript client. A dependency here is how an EF entity ends up
    /// being returned to a browser, which is what the legacy system did for most of
    /// its endpoints — it had 41 DTOs for 146 entities.
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
