using Erp.BuildingBlocks.Application.DependencyInjection;
using Erp.BuildingBlocks.Persistence.DependencyInjection;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Masters;

/// <summary>
/// Master data: parts, and in later phases suppliers, customers, business units,
/// locations, units of measure and the financial-year calendar.
/// <para>
/// Public because the host discovers and instantiates it. The application code in
/// this assembly — handlers, endpoints, validators — stays <c>internal</c> except
/// <c>Integration/</c>. Entities and the <c>ErpDbContext</c> they map to live in
/// <c>Erp.Persistence</c>, which every module shares.
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

        // No DbContext registration here. There is one ErpDbContext for the whole
        // application and the host registers it — see AddErpDbContext.

        // Discovered, not listed. See HandlerRegistration.
        services.AddHandlersFromAssembly(typeof(MastersModule).Assembly);

        services.AddValidatorsFromAssemblyContaining<MastersModule>(includeInternalTypes: true);
    }
}
