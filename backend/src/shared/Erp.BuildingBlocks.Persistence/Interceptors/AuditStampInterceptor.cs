using Erp.BuildingBlocks.Application.Abstractions;
using Erp.SharedKernel.Identity;
using Erp.SharedKernel.Primitives;
using Erp.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Erp.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Stamps created/modified metadata on every <see cref="IAuditable"/> entity.
/// <para>
/// Implementing the interface is the entire opt-in. There is no call site to
/// forget, and no way to save an auditable entity without an author and a timestamp.
/// </para>
/// </summary>
public sealed class AuditStampInterceptor(ICurrentUser currentUser, IClock clock) : SaveChangesInterceptor
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

        var now = clock.UtcNow;
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : SystemUsers.Background;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.CreatedByUserId = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = now;
                    entry.Entity.ModifiedByUserId = userId;

                    // Creation metadata is immutable: without this, a detached
                    // entity round-tripped from a client could rewrite its own history.
                    entry.Property(e => e.CreatedAtUtc).IsModified = false;
                    entry.Property(e => e.CreatedByUserId).IsModified = false;
                    break;

                default:
                    break;
            }
        }
    }
}
