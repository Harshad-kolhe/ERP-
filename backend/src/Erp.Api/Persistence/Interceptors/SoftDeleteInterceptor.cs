using Erp.Api.Common.Security;
using Erp.Api.Common.Identity;
using Erp.Api.Common.Entities;
using Erp.Api.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Erp.Api.Persistence.Interceptors;

/// <summary>
/// Turns a delete into an update on every <see cref="ISoftDeletable"/> entity.
/// <para>
/// Handlers call <c>Remove</c> as normal and never think about it. Nothing is
/// physically destroyed, which matters in a system where a purchase order deleted
/// in error is a commercial dispute rather than an inconvenience.
/// </para>
/// </summary>
public sealed class SoftDeleteInterceptor(ICurrentUser currentUser, IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Convert(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Convert(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : SystemUsers.Background;

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = now;
            entry.Entity.DeletedByUserId = userId;
        }
    }
}
