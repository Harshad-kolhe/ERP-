using Erp.BuildingBlocks.Application.Abstractions;
using Erp.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Erp.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Assigns the current business unit to new tenant-scoped rows.
/// <para>
/// Paired with the global query filter in <see cref="ErpDbContextBase"/>: the
/// filter controls what a request can read, this controls what it writes. Without
/// the stamp, a row could be created that its own author could not then see.
/// </para>
/// </summary>
public sealed class BusinessUnitStampInterceptor(IBusinessUnitContext businessUnitContext) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<IBusinessUnitScoped>())
        {
            if (entry.State != EntityState.Added)
            {
                // A row never changes tenant. Reassigning one is a data-migration
                // operation, not something an ordinary save may do implicitly.
                if (entry.State == EntityState.Modified)
                {
                    entry.Property(e => e.BusinessUnitId).IsModified = false;
                }

                continue;
            }

            if (entry.Entity.BusinessUnitId == 0)
            {
                entry.Entity.BusinessUnitId = businessUnitContext.BusinessUnitId;
            }
        }
    }
}
