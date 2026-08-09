using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Authentication;

/// <summary>
/// Identity storage, in its own <c>identity</c> schema and with its own migration
/// history — the same isolation every module gets.
/// </summary>
public sealed class IdentityDataContext(DbContextOptions<IdentityDataContext> options)
    : IdentityDbContext<ErpUser, ErpRole, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasDefaultSchema("identity");

        base.OnModelCreating(builder);
    }
}
