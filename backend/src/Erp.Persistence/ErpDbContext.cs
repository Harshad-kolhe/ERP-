using System.Reflection;
using Erp.BuildingBlocks.Application.Abstractions;
using Erp.Persistence.Domain.Assemblies;
using Erp.Persistence.Domain.BusinessUnits;
using Erp.Persistence.Domain.Customers;
using Erp.Persistence.Domain.Employees;
using Erp.Persistence.Domain.HsnCodes;
using Erp.Persistence.Domain.Lookups;
using Erp.Persistence.Domain.ParentParts;
using Erp.Persistence.Domain.Parts;
using Erp.Persistence.Domain.Roles;
using Erp.Persistence.Domain.Suppliers;
using Erp.Persistence.Domain.UnitsOfMeasure;
using Erp.Persistence.Identity;
using Erp.SharedKernel.Primitives;
using Erp.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Erp.Persistence;

/// <summary>
/// The application's single <see cref="DbContext"/>. Every entity in the system —
/// master data and identity alike — is mapped here, in one model with one
/// migration history.
/// <para>
/// Tables keep their per-area schemas: master data in <c>masters</c>, identity in
/// <c>identity</c>. Ownership stays legible in the database even though one class
/// now maps all of it, and a query may join freely across the two — which is why
/// the audit-name view this replaces is gone: a "created by" name is now an
/// ordinary join onto <see cref="Users"/>.
/// </para>
/// <para>
/// Everything cross-cutting is applied <em>by convention</em>, by walking the
/// model — never per entity. That distinction is the whole point. The system this
/// replaces isolated tenants with a hand-called <c>.ApplyBu()</c> LINQ extension,
/// and every query where a developer forgot to call it silently returned another
/// business unit's data. A convention cannot be forgotten: an entity that
/// implements <see cref="IBusinessUnitScoped"/> is filtered whether or not anyone
/// remembered it existed.
/// </para>
/// </summary>
public sealed class ErpDbContext(
    DbContextOptions<ErpDbContext> options,
    IBusinessUnitContext businessUnitContext)
    : IdentityDbContext<ErpUser, ErpRole, Guid>(options)
{
    /// <summary>Default schema. Identity's own tables are remapped to <c>identity</c> below.</summary>
    private const string MastersSchema = "masters";

    private const string IdentitySchema = "identity";

    private static readonly MethodInfo ApplyFiltersMethod =
        typeof(ErpDbContext).GetMethod(
            nameof(ApplyGlobalFilters),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    public DbSet<Part> Parts => Set<Part>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Role> MasterRoles => Set<Role>();

    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();

    /// <summary>
    /// Sections, assemblies and sub-assemblies — one table for all three levels.
    /// See <see cref="AssemblyNode"/> for why they are not three tables.
    /// </summary>
    public DbSet<AssemblyNode> AssemblyNodes => Set<AssemblyNode>();

    /// <summary>
    /// Parent parts. Their component lines are deliberately <em>not</em> exposed as
    /// a set: a line is only meaningful inside its build, and a top-level
    /// <c>DbSet</c> would be a way to query them without the tenancy filter that
    /// lives on the header.
    /// </summary>
    public DbSet<ParentPart> ParentParts => Set<ParentPart>();

    /// <summary>
    /// Every dropdown option in the system. See <see cref="LookupValue"/> for why
    /// none of them live in source.
    /// </summary>
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();

    /// <summary>
    /// Units of measure. A list of options in <see cref="LookupValue"/> until it
    /// needed conversion factors and precision — see <see cref="UnitOfMeasure"/>.
    /// </summary>
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    /// <summary>
    /// HSN codes and their GST rate history. Their rates are deliberately
    /// <em>not</em> a set of their own: a rate is only meaningful inside the code it
    /// belongs to, and a top-level <c>DbSet</c> would be a way to read one without
    /// the code that explains it.
    /// </summary>
    public DbSet<HsnCode> HsnCodes => Set<HsnCode>();

    // Read inside compiled query filters. EF treats these as query parameters and
    // re-evaluates them per request, so one cached plan serves every business unit.
    private int CurrentBusinessUnitId => businessUnitContext.BusinessUnitId;

    private bool CanAccessAllBusinessUnits => businessUnitContext.CanAccessAllBusinessUnits;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasDefaultSchema(MastersSchema);

        // Identity's own mappings, then the entity configurations, then the
        // cross-cutting conventions over whatever the two produced.
        base.OnModelCreating(builder);

        MapIdentityToItsOwnSchema(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (entityType.IsOwned() || clrType is null)
            {
                continue;
            }

            // Optimistic concurrency: a stale write becomes HTTP 409 instead of a
            // silent last-writer-wins overwrite.
            if (typeof(IHasRowVersion).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType)
                    .Property(nameof(IHasRowVersion.RowVersion))
                    .IsRowVersion();
            }

            // Deleted rows and other tenants' rows disappear from every query,
            // including ones written years from now by someone who never read this file.
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType)
                || typeof(IBusinessUnitScoped).IsAssignableFrom(clrType))
            {
                ApplyFiltersMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                // Partial index: soft-deleted rows are excluded from the hot path.
                builder.Entity(clrType)
                    .HasIndex(nameof(ISoftDeletable.IsDeleted))
                    .HasFilter("[IsDeleted] = 0");
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Money scale by default. Quantity columns override this to (18,6)
        // explicitly in their entity configuration — see Quantity.Scale.
        configurationBuilder.Properties<decimal>().HavePrecision(18, Money.Scale);

        base.ConfigureConventions(configurationBuilder);
    }

    /// <summary>
    /// Keeps the ASP.NET Identity tables in <c>identity</c> rather than letting them
    /// fall into the default schema. Named explicitly because <c>HasDefaultSchema</c>
    /// applies to the whole model, and identity's tables are the one area that does
    /// not belong to master data.
    /// </summary>
    private static void MapIdentityToItsOwnSchema(ModelBuilder builder)
    {
        builder.Entity<ErpUser>().ToTable("AspNetUsers", IdentitySchema);
        builder.Entity<ErpRole>().ToTable("AspNetRoles", IdentitySchema);
        builder.Entity<IdentityUserRole<Guid>>().ToTable("AspNetUserRoles", IdentitySchema);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("AspNetUserClaims", IdentitySchema);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("AspNetUserLogins", IdentitySchema);
        builder.Entity<IdentityUserToken<Guid>>().ToTable("AspNetUserTokens", IdentitySchema);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("AspNetRoleClaims", IdentitySchema);
    }

    /// <summary>
    /// Builds the combined soft-delete and tenancy filter. EF Core permits one
    /// filter expression per entity type, so the two predicates are composed here
    /// rather than registered separately.
    /// </summary>
    private void ApplyGlobalFilters<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity));
        var isTenantScoped = typeof(IBusinessUnitScoped).IsAssignableFrom(typeof(TEntity));

        // EF.Property is used instead of an interface cast: it always translates to
        // the mapped column, whereas a cast can defeat the query translator.
        if (isSoftDeletable && isTenantScoped)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                !EF.Property<bool>(e, nameof(ISoftDeletable.IsDeleted))
                && (CanAccessAllBusinessUnits
                    || EF.Property<int>(e, nameof(IBusinessUnitScoped.BusinessUnitId)) == CurrentBusinessUnitId));
        }
        else if (isSoftDeletable)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                !EF.Property<bool>(e, nameof(ISoftDeletable.IsDeleted)));
        }
        else if (isTenantScoped)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                CanAccessAllBusinessUnits
                || EF.Property<int>(e, nameof(IBusinessUnitScoped.BusinessUnitId)) == CurrentBusinessUnitId);
        }
    }
}
