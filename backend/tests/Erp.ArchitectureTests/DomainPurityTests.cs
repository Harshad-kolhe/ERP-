using System.Reflection;
using Erp.Api.Persistence;

namespace Erp.ArchitectureTests;

public sealed class DomainPurityTests
{
    private const string DomainNamespace = "Erp.Api.Domain.";

    private const string IdentityNamespace = "Erp.Api.Domain.Identity";

    private static readonly Assembly ApplicationAssembly = typeof(ErpDbContext).Assembly;

    [Fact]
    public void Domain_types_do_not_reference_infrastructure()
    {
        var violations = new List<string>();

        foreach (var type in DomainTypes())
        {
            foreach (var referenced in ReferencedTypes(type))
            {
                var referencedAssembly = referenced.Assembly.GetName().Name ?? string.Empty;

                if (referencedAssembly.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                    || referencedAssembly.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
                {
                    violations.Add($"{type.FullName} references {referenced.FullName}");
                }
            }
        }

        var distinct = violations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        distinct.ShouldBeEmpty(
            "Domain/ holds the business rules and must stay unit-testable without a database or a "
            + "request. A domain type that references EF Core or ASP.NET Core is how the legacy "
            + "Models/BLL/ layer became the data layer. Move the dependency into a Feature service:\n"
            + string.Join('\n', distinct));
    }

    [Fact]
    public void Domain_scan_actually_finds_types()
    {
        DomainTypes().Count.ShouldBeGreaterThan(20,
            "this guards the guard above. If Domain/ is renamed or moved, the purity test "
            + "silently passes over an empty set, which is what it did before the projects were merged.");
    }

    private static List<Type> DomainTypes() =>
        [.. ApplicationAssembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith(DomainNamespace, StringComparison.Ordinal) == true)
            .Where(type => type.Namespace?.StartsWith(IdentityNamespace, StringComparison.Ordinal) != true)];

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

        foreach (var field in type.GetFields(All))
        {
            yield return field.FieldType;
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
}
