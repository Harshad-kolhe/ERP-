using System.Text.Json.Serialization;
using Erp.Api.Administration;
using Erp.Api.Authentication;
using Erp.Api.Extensions;
using Erp.Api.Middleware;
using Erp.BuildingBlocks.Persistence.DependencyInjection;
using Erp.BuildingBlocks.Persistence.Time;
using Erp.BuildingBlocks.Web.DependencyInjection;
using Erp.BuildingBlocks.Web.Modules;
using Erp.BuildingBlocks.Web.Security;
using Erp.Persistence.DependencyInjection;
using Erp.SharedKernel.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureErpSerilog();

builder.Services.AddErpObservability(builder.Configuration);
builder.Services.AddErpAuthentication();
builder.Services.AddErpWebBuildingBlocks();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IClock>(
    new SystemClock(TimeProvider.System, BusinessTimeZone.Resolve(builder.Configuration)));

// Audit stamping, tenant stamping and soft delete. AddErpDbContext attaches them
// with .AddErpInterceptors(serviceProvider) — explicitly, because EF's implicit
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

builder.Services.AddOpenApi();

// Validators defined by the host itself — the role screens. Modules register their own.
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

// The host defines permissions too, for administering the security model. Modules
// are found by assembly scan; this one is registered by hand because it lives here.
builder.Services.AddSingleton<IPermissionSource, AdminPermissionSource>();

// Every module registers itself. Nothing here names one.
var modules = builder.Services.AddErpModules(builder.Configuration, typeof(Program).Assembly);

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

app.MapErpModules(modules);

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
