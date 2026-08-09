using Erp.BuildingBlocks.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.BuildingBlocks.Persistence.DependencyInjection;

public static class PersistenceExtensions
{
    private const string ConnectionStringName = "Erp";

    /// <summary>
    /// The database every module connects to. Every <c>UseSqlServer</c> call goes
    /// through here rather than reading the configuration key directly.
    /// <para>
    /// The guard exists because the missing-configuration case was unreadable.
    /// <c>UseSqlServer(null)</c> is accepted without complaint and fails much later,
    /// on first connection, as <c>"The ConnectionString property has not been
    /// initialized"</c> thrown from inside SqlClient — a stack trace that names
    /// neither the setting nor the application. On a freshly cloned repository that
    /// is the first thing a new developer sees, and connection strings are per-machine
    /// by design, so it is the one setup step that can never be committed for them.
    /// </para>
    /// </summary>
    public static string ErpConnectionString(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        // Whitespace as well as null: an empty `ConnectionStrings__Erp=` line in a
        // .env file reaches here as "", which passes a null check and then fails
        // exactly the same way, in exactly the same unreadable place.
        return !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new InvalidOperationException(
                $"ConnectionStrings:{ConnectionStringName} is not configured. In development set it with "
                + $"`dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<connection string>\"` "
                + $"from backend/src/Erp.Api, or export ConnectionStrings__{ConnectionStringName}. "
                + "See docs/local-setup.md §3.");
    }

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
