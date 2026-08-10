using System.Text.Json.Serialization;
using DotNetEnv;
using Erp.Api.Administration;
using Erp.Api.Authentication;
using Erp.Api.Extensions;
using Erp.Api.Middleware;
using Erp.Api.Persistence;
using Erp.Api.Common.Time;
using Erp.Api.Common.Web;
using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Security;
using Erp.Api.Common.Validation;
using Erp.Api.Features;
using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.AspNetCore;
using Serilog;

// `.env` â†’ environment variables, before the configuration builder reads them.
// One gitignored file per machine holds every local setting, so onboarding is a
// copy of `.env.example` rather than one `dotnet user-secrets set` per key, and a
// new shared variable arrives with the pull request that needs it.
//
// TraversePath: found by walking up from the working directory, so the same file
// serves `dotnet run` from backend/ and `dotnet ef` from src/Erp.Api/.
//
// NoClobber: a real environment variable always wins. CI and every deployed
// environment therefore behave exactly as if no file existed, and a stale .env
// left on a server cannot quietly override what the platform supplies. Missing
// file is not an error â€” that is the normal case everywhere but a laptop.
Env.TraversePath().NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureErpSerilog();

builder.Services.AddErpObservability(builder.Configuration);
builder.Services.AddErpAuthentication();
builder.Services.AddErpWebBuildingBlocks();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IClock>(
    new SystemClock(TimeProvider.System, BusinessTimeZone.Resolve(builder.Configuration)));

// Audit stamping, tenant stamping and soft delete. AddErpDbContext attaches them
// with .AddErpInterceptors(serviceProvider) â€” explicitly, because EF's implicit
// discovery of IInterceptor registrations did not pick them up and the failure was
// silent: rows written with BusinessUnitId = 0 and no audit trail.
builder.Services.AddErpPersistence();

// One DbContext for the whole application. Modules map their tables into it
// through IEntityTypeConfiguration; none of them registers a context of its own.
builder.Services.AddErpDbContext(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
    // Enums travel as names, never ordinals. An ordinal on the wire is what turns
    // a status column into the literal "02" that nobody can read three years later.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationActionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddErpOpenApi();

// Validators defined by the host itself â€” the role screens. Modules register their own.
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

builder.Services.AddHandlersFromAssembly(typeof(Program).Assembly);
builder.Services.AddFeatureServices(typeof(Program).Assembly);

builder.Services.AddSingleton<IPermissionSource, AdminPermissionSource>();
builder.Services.AddSingleton<IPermissionSource, MastersPermissionSource>();
builder.Services.AddSingleton<IPermissionCatalogue, PermissionCatalogue>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapAuthEndpoints();

// Roles live here while Identity does. They move to Erp.Modules.Identity with it.
app.MapRoleEndpoints();

app.MapMasters();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Applies migrations and seeds one administrator. No-op outside Development.
await app.BootstrapDevelopmentAsync();

await app.RunAsync();

/// <summary>Exposed so integration tests can boot the real host via WebApplicationFactory.</summary>
public partial class Program;
