using Erp.BuildingBlocks.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.BuildingBlocks.Persistence.DependencyInjection;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers the cross-cutting save interceptors. Call once from the host.
    /// </summary>
    public static IServiceCollection AddErpPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped: each depends on the request's ICurrentUser and IBusinessUnitContext.
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<BusinessUnitStampInterceptor>();
        services.AddScoped<AuditStampInterceptor>();

        return services;
    }

    /// <summary>
    /// Attaches the interceptors to a module's <see cref="DbContext"/>.
    /// <para>
    /// Every module must call this. It is explicit rather than relying on EF's
    /// implicit discovery of <c>IInterceptor</c> registrations, which did not pick
    /// these up: the result was rows silently written with <c>BusinessUnitId = 0</c>
    /// and no audit stamps, invisible to the tenant filter that had just created
    /// them. Explicit registration also fixes the order, which matters.
    /// </para>
    /// </summary>
    public static DbContextOptionsBuilder AddErpInterceptors(
        this DbContextOptionsBuilder options,
        IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(provider);

        // Order is significant. Soft delete rewrites a Deleted entry into a Modified
        // one; the audit stamp only stamps entries that are already Modified, so it
        // has to run afterwards or a soft delete would carry no modification record.
        return options.AddInterceptors(
            Resolve<SoftDeleteInterceptor>(provider),
            Resolve<BusinessUnitStampInterceptor>(provider),
            Resolve<AuditStampInterceptor>(provider));
    }

    // IServiceProvider.GetService(Type) rather than the GetRequiredService extension,
    // which is a banned symbol: hand-resolving services hides dependencies. This is
    // the composition of a DbContext's options, where resolution is the job.
    private static IInterceptor Resolve<TInterceptor>(IServiceProvider provider)
        where TInterceptor : IInterceptor =>
        provider.GetService(typeof(TInterceptor)) as IInterceptor
        ?? throw new InvalidOperationException(
            $"{typeof(TInterceptor).Name} is not registered. Call services.AddErpPersistence() in the host.");
}
