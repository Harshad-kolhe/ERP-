using System.Reflection;
using Erp.BuildingBlocks.Application.Abstractions;
using Erp.SharedKernel.Primitives;
using Erp.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Erp.BuildingBlocks.Persistence;

/// <summary>
/// Base class for every module's <see cref="DbContext"/>.
/// <para>
/// Everything cross-cutting is applied here <em>by convention</em>, by walking the
/// model — never per entity. That distinction is the whole point. The system this
/// replaces isolated tenants with a hand-called <c>.ApplyBu()</c> LINQ extension,
/// and every query where a developer forgot to call it silently returned another
/// business unit's data. A convention cannot be forgotten: an entity that
/// implements <see cref="IBusinessUnitScoped"/> is filtered whether or not anyone
/// remembered it existed.
/// </para>
/// </summary>
public abstract class ErpDbContextBase(
    DbContextOptions options,
    IBusinessUnitContext businessUnitContext) : DbContext(options)
{
    private static readonly MethodInfo ApplyFiltersMethod =
        typeof(ErpDbContextBase).GetMethod(
            nameof(ApplyGlobalFilters),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// SQL Server schema this module owns, e.g. <c>masters</c>, <c>inventory</c>.
    /// One schema per module keeps ownership legible in the database itself.
    /// </summary>
    protected abstract string Schema { get; }

    // Read inside compiled query filters. EF treats these as query parameters and
    // re-evaluates them per request, so one cached plan serves every business unit.
    private int CurrentBusinessUnitId => businessUnitContext.BusinessUnitId;

    private bool CanAccessAllBusinessUnits => businessUnitContext.CanAccessAllBusinessUnits;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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
                modelBuilder.Entity(clrType)
                    .Property(nameof(IHasRowVersion.RowVersion))
                    .IsRowVersion();
            }

            // Deleted rows and other tenants' rows disappear from every query,
            // including ones written years from now by someone who never read this file.
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType)
                || typeof(IBusinessUnitScoped).IsAssignableFrom(clrType))
            {
                ApplyFiltersMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                // Partial index: soft-deleted rows are excluded from the hot path.
                modelBuilder.Entity(clrType)
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
