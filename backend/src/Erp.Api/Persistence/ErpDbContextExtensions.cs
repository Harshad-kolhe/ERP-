using Erp.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Api.Persistence;

public static class ErpDbContextExtensions
{
    /// <summary>
    /// Registers the application's single <see cref="ErpDbContext"/>. Called once
    /// from the host â€” modules no longer register a context of their own, so there
    /// is exactly one place that names the connection string and one migration
    /// history table for the whole schema.
    /// </summary>
    public static IServiceCollection AddErpDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ErpDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(
                    configuration.ErpConnectionString(),
                    sql => sql
                        .MigrationsHistoryTable("__EFMigrationsHistory", "masters")
                        .EnableRetryOnFailure())
                // Audit stamping, tenant stamping and soft delete. Not optional.
                .AddErpInterceptors(serviceProvider));

        return services;
    }
}
