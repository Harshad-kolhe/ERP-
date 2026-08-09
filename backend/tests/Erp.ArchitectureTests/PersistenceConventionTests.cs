using Erp.Persistence;
using Erp.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.ArchitectureTests;

/// <summary>
/// Checks the EF model that was actually built, not the configuration source.
/// <para>
/// These are the conventions that must never be forgotten on a new entity. Because
/// they are asserted against the model, adding an entity that skips one fails the
/// build rather than quietly shipping a table with no tenant filter.
/// </para>
/// </summary>
public sealed class PersistenceConventionTests(ErpTestHost host) : IClassFixture<ErpTestHost>
{
    /// <summary>
    /// The whole model, from the one context. There is nothing to enumerate any
    /// more: every module maps its tables into <see cref="ErpDbContext"/>, so a
    /// convention checked here is checked everywhere.
    /// </summary>
    private IReadOnlyList<IEntityType> EntityTypes
    {
        get
        {
            using var scope = host.Services.CreateScope();

            return scope.ServiceProvider.GetService(typeof(ErpDbContext)) is ErpDbContext context
                ? [.. context.Model.GetEntityTypes()]
                : [];
        }
    }

    /// <summary>
    /// Replaces the ineffective <c>T:System.Single</c> banned-symbol entry: the
    /// banned-API analyzer does not match predefined type keywords, so that rule
    /// looked like a guardrail while never firing. This checks the place the
    /// precision loss would actually occur — a mapped column.
    /// </summary>
    [Fact]
    public void No_mapped_property_uses_floating_point()
    {
        var offenders = EntityTypes
            .SelectMany(entity => entity.GetProperties()
                .Where(property =>
                {
                    var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    return clrType == typeof(float) || clrType == typeof(double);
                })
                .Select(property => $"{entity.ClrType.Name}.{property.Name} ({property.ClrType.Name})"))
            .ToList();

        offenders.ShouldBeEmpty(
            "money and quantity must be decimal — float and double cannot represent them exactly:\n"
            + string.Join('\n', offenders));
    }

    /// <summary>
    /// The tenancy filter is the single most important convention in the system.
    /// The legacy equivalent was a hand-called <c>.ApplyBu()</c> extension, and every
    /// query that forgot it returned another business unit's data.
    /// </summary>
    [Fact]
    public void Every_tenant_scoped_entity_has_a_query_filter()
    {
        var offenders = EntityTypes
            .Where(entity => typeof(IBusinessUnitScoped).IsAssignableFrom(entity.ClrType))
            .Where(entity => entity.GetDeclaredQueryFilters().Count == 0)
            .Select(entity => entity.ClrType.FullName!)
            .ToList();

        offenders.ShouldBeEmpty(
            "these tenant-scoped entities have no global query filter:\n" + string.Join('\n', offenders));
    }

    /// <summary>A rowversion that is not configured as a concurrency token silently allows last-writer-wins.</summary>
    [Fact]
    public void Every_versioned_entity_has_a_concurrency_token()
    {
        var offenders = EntityTypes
            .Where(entity => typeof(IHasRowVersion).IsAssignableFrom(entity.ClrType))
            .Where(entity => entity.FindProperty(nameof(IHasRowVersion.RowVersion))?.IsConcurrencyToken != true)
            .Select(entity => entity.ClrType.FullName!)
            .ToList();

        offenders.ShouldBeEmpty(
            "these entities implement IHasRowVersion but their RowVersion is not a concurrency token:\n"
            + string.Join('\n', offenders));
    }

    /// <summary>An unspecified decimal precision defaults to (18,2) on SQL Server, which silently truncates quantities.</summary>
    [Fact]
    public void Every_decimal_property_declares_its_precision()
    {
        var offenders = EntityTypes
            .SelectMany(entity => entity.GetProperties()
                .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(decimal))
                .Where(property => property.GetPrecision() is null)
                .Select(property => $"{entity.ClrType.Name}.{property.Name}"))
            .ToList();

        offenders.ShouldBeEmpty(
            "decimal columns must declare precision explicitly:\n" + string.Join('\n', offenders));
    }

    /// <summary>Guards the guard: an empty model would make every test above vacuously true.</summary>
    [Fact]
    public void Model_contains_entities()
    {
        EntityTypes.ShouldNotBeEmpty("no entity types were found — the DbContext resolution in this fixture is broken.");
    }
}
