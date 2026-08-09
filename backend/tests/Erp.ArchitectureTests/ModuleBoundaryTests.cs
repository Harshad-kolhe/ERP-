using System.Reflection;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters;

namespace Erp.ArchitectureTests;

/// <summary>
/// Enforces that a module is a real boundary and not a folder name.
/// <para>
/// The legacy solution had <c>Domain/</c>, <c>Models/BLL/</c> and
/// <c>Models/Database/</c> directories that implied a layered architecture while
/// every class could reach every other one — <c>Domain/</c> ended up containing a
/// single file. Naming a boundary does nothing; the compiler enforcing it does.
/// </para>
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly Assembly[] ModuleAssemblies = [typeof(MastersModule).Assembly];

    /// <summary>
    /// A module exposes its <see cref="IModule"/> entry point and its
    /// <c>Integration</c> folder. Everything else — entities, handlers, the
    /// DbContext, endpoints — is invisible outside the assembly, so another module
    /// physically cannot take a dependency on it.
    /// </summary>
    [Fact]
    public void Module_types_are_internal_except_the_module_entry_point_and_Integration()
    {
        var leaked = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                if (typeof(IModule).IsAssignableFrom(type))
                {
                    continue;
                }

                var ns = type.Namespace ?? string.Empty;

                if (ns.Contains(".Integration", StringComparison.Ordinal))
                {
                    continue;
                }

                // EF's migration template emits `public partial class`. Their
                // visibility is the tool's convention, not a design decision, and
                // nothing outside the module can meaningfully use them.
                if (ns.Contains(".Migrations", StringComparison.Ordinal))
                {
                    continue;
                }

                leaked.Add(type.FullName!);
            }
        }

        leaked.ShouldBeEmpty(
            "these module types are public but live outside Integration/. Make them internal, "
            + "or move them to Integration/ if another module genuinely needs them:\n"
            + string.Join('\n', leaked));
    }

    /// <summary>
    /// Domain classes stay free of infrastructure. A domain that references
    /// <c>DbContext</c> or <c>HttpContext</c> cannot be unit-tested without one, and
    /// the legacy "BLL" layer — which injected the DbContext directly and therefore
    /// simply <em>was</em> the data layer — is what that looks like at scale.
    /// </summary>
    [Fact]
    public void Domain_types_do_not_reference_infrastructure()
    {
        var violations = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var domainTypes = assembly.GetTypes()
                .Where(type => type.Namespace?.Contains(".Domain.", StringComparison.Ordinal) == true);

            foreach (var type in domainTypes)
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
        }

        violations.Distinct(StringComparer.Ordinal).ToList().ShouldBeEmpty(
            "domain types must not reference EF Core or ASP.NET Core:\n"
            + string.Join('\n', violations.Distinct(StringComparer.Ordinal)));
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
