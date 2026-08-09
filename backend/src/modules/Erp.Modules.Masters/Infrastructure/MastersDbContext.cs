using Erp.BuildingBlocks.Application.Abstractions;
using Erp.BuildingBlocks.Persistence;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Domain.BusinessUnits;
using Erp.Modules.Masters.Domain.Customers;
using Erp.Modules.Masters.Domain.Employees;
using Erp.Modules.Masters.Domain.Lookups;
using Erp.Modules.Masters.Domain.ParentParts;
using Erp.Modules.Masters.Domain.Parts;
using Erp.Modules.Masters.Domain.Roles;
using Erp.Modules.Masters.Domain.Suppliers;
using Erp.Modules.Masters.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Infrastructure;

/// <summary>
/// Persistence for the Masters module. One context per module, one schema per
/// context, its own migration history.
/// <para>
/// The legacy system had a single <c>DbContext</c> with 284 <c>DbSet</c>
/// properties, 188 of them keyless projections of stored procedures, in one
/// 47 KB file that every feature had to touch. Splitting per module means a
/// change to Inventory cannot break a Masters query, and each module's schema is
/// legible in the database on its own.
/// </para>
/// </summary>
internal sealed class MastersDbContext(
    DbContextOptions<MastersDbContext> options,
    IBusinessUnitContext businessUnitContext)
    : ErpDbContextBase(options, businessUnitContext)
{
    protected override string Schema => "masters";

    public DbSet<Part> Parts => Set<Part>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Role> Roles => Set<Role>();

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
    /// Every dropdown option in the module. See <see cref="LookupValue"/> for why
    /// none of them live in source.
    /// </summary>
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();

    /// <summary>
    /// Read-only names for the audit columns. See <see cref="AuditUser"/> for why a
    /// view over identity's table beats the alternatives.
    /// </summary>
    public DbSet<AuditUser> AuditUsers => Set<AuditUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Entity configurations first, then the base class applies the
        // cross-cutting conventions (tenancy filter, soft delete, rowversion)
        // over whatever the configurations produced.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MastersDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
