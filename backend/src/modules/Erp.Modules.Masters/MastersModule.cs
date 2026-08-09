using Erp.BuildingBlocks.Application.DependencyInjection;
using Erp.BuildingBlocks.Persistence.DependencyInjection;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Masters;

/// <summary>
/// Master data: parts, and in later phases suppliers, customers, business units,
/// locations, units of measure and the financial-year calendar.
/// <para>
/// Public because the host discovers and instantiates it. Everything else in this
/// assembly is <c>internal</c> except <c>Integration/</c>, so no other module can
/// reference a Masters entity, handler or DbContext even by accident.
/// </para>
/// </summary>
public sealed class MastersModule : ModuleBase
{
    public override string Name => "Masters";

    protected override string RoutePrefix => "masters";

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<MastersDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(
                    configuration.GetConnectionString("Erp"),
                    sql => sql
                        // Per-module history table, in the module's own schema, so
                        // modules can be migrated independently of each other.
                        .MigrationsHistoryTable("__EFMigrationsHistory", "masters")
                        .EnableRetryOnFailure())
                // Audit stamping, tenant stamping and soft delete. Not optional.
                .AddErpInterceptors(serviceProvider));

        // Discovered, not listed. See HandlerRegistration.
        services.AddHandlersFromAssembly(typeof(MastersModule).Assembly);

        services.AddValidatorsFromAssemblyContaining<MastersModule>(includeInternalTypes: true);
    }
}
