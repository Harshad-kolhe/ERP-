using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.ArchitectureTests.Documentation;

/// <summary>
/// Regenerates <c>db/erd/</c> from the EF Core model the application actually built.
/// <para>
/// The schema's only other description is a set of migrations and two model
/// snapshots — accurate, and unreadable. The obvious fix, drawing an ERD and
/// updating it as modules land, reproduces the failure this repository is built to
/// avoid: a diagram nobody is forced to update is wrong within one phase, and a
/// wrong ERD is worse than none because people trust it.
/// </para>
/// <para>
/// So it is generated, and it discovers contexts rather than listing them. A new
/// module ships a <see cref="DbContext"/>, and its tables appear here on the next
/// test run with no diagram work at all.
/// </para>
/// <para>
/// No database is contacted — EF builds the model from the provider and the
/// configuration, and nothing here opens a connection.
/// </para>
/// </summary>
public sealed class SchemaDiagramTests(ErpTestHost host) : IClassFixture<ErpTestHost>
{
    private const string OutputDirectory = "db/erd";

    /// <summary>
    /// Writing files from a test is unusual, and deliberate: regenerating the
    /// diagrams costs nothing on top of a suite that already boots the host, so
    /// anyone who runs the tests leaves with current documentation. The targeted
    /// command is in <c>README.md</c>.
    /// </summary>
    [Fact]
    public void Schema_diagrams_regenerate_from_the_model()
    {
        var directory = Directory.CreateDirectory(
            Path.Combine(RepositoryPaths.RepositoryRoot().FullName, OutputDirectory));

        var written = new List<string>();

        foreach (var (schema, entityTypes) in EntityTypesBySchema())
        {
            var path = Path.Combine(directory.FullName, $"{schema}.md");

            // '\n' explicitly rather than File.WriteAllText's platform default: the
            // file must be byte-identical whichever machine regenerated it.
            File.WriteAllText(path, MermaidErd.Render(schema, entityTypes));
            written.Add(path);
        }

        written.ShouldNotBeEmpty("no schema was discovered — the DbContext resolution in this fixture is broken.");

        RemoveDiagramsForSchemasThatNoLongerExist(directory, written);

        foreach (var path in written)
        {
            new FileInfo(path).Length.ShouldBeGreaterThan(0, $"{path} was written empty.");
        }
    }

    /// <summary>
    /// The property that lets the diagrams be committed: regenerating without a model
    /// change must produce identical bytes, or every unrelated pull request carries
    /// diagram churn and nobody reads the diff.
    /// </summary>
    [Fact]
    public void Rendering_the_same_model_twice_produces_identical_output()
    {
        foreach (var (schema, entityTypes) in EntityTypesBySchema())
        {
            MermaidErd.Render(schema, entityTypes)
                .ShouldBe(MermaidErd.Render(schema, entityTypes), $"the {schema} diagram is not deterministic.");
        }
    }

    /// <summary>
    /// Guards the guard: a discovery scan that silently found nothing would make the
    /// generator emit a confidently empty diagram.
    /// </summary>
    [Fact]
    public void Every_owned_schema_is_discovered()
    {
        var schemas = EntityTypesBySchema().Select(entry => entry.Schema).ToList();

        schemas.ShouldContain("masters");
        schemas.ShouldContain(
            "identity",
            "Identity's context lives in Erp.Api, not a module — discovery must not be scoped to module assemblies.");
    }

    /// <summary>
    /// Finds every context the same way the host itself does in
    /// <c>DevelopmentBootstrap</c>: by type across all <c>Erp.*</c> assemblies, so no
    /// module has to be named here and none can be forgotten.
    /// </summary>
    private List<(string Schema, IReadOnlyCollection<IEntityType> EntityTypes)> EntityTypesBySchema()
    {
        using var scope = host.Services.CreateScope();

        var contextTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Erp.", StringComparison.Ordinal) == true)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(DbContext).IsAssignableFrom(type) && !type.IsAbstract)
            .Distinct();

        var entityTypes = new List<IEntityType>();

        foreach (var contextType in contextTypes)
        {
            if (scope.ServiceProvider.GetService(contextType) is DbContext context)
            {
                entityTypes.AddRange(context.Model.GetEntityTypes());
            }
        }

        // GetSchema() is the *table* schema and is null for an entity mapped to a
        // view — Masters reads identity's user table that way, to put a name in the
        // "Created by" columns. Without the view fallback such an entity lands in a
        // "dbo" bucket it does not belong to, and the generator writes an empty
        // dbo.md next to the real schemas.
        return [.. entityTypes
            .GroupBy(
                entityType => entityType.GetSchema() ?? entityType.GetViewSchema() ?? "dbo",
                StringComparer.Ordinal)
            .Select(group => (Schema: group.Key, EntityTypes: (IReadOnlyCollection<IEntityType>)[.. group]))
            .OrderBy(entry => entry.Schema, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Deletes diagrams for schemas that no longer exist, identified by the generated
    /// banner so a hand-written file in the same folder is never touched.
    /// </summary>
    private static void RemoveDiagramsForSchemasThatNoLongerExist(DirectoryInfo directory, List<string> written)
    {
        var current = written.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in directory.EnumerateFiles("*.md"))
        {
            if (current.Contains(file.FullName))
            {
                continue;
            }

            using (var reader = file.OpenText())
            {
                if (reader.ReadLine() != MermaidErd.GeneratedBanner)
                {
                    continue;
                }
            }

            file.Delete();
        }
    }
}
