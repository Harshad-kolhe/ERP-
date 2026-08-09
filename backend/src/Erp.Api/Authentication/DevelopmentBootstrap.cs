using System.Security.Claims;
using System.Security.Cryptography;
using Erp.BuildingBlocks.Web.Security;
using Erp.Persistence;
using Erp.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Authentication;

/// <summary>
/// Applies migrations and creates one way in, in Development only.
/// <para>
/// This is a <em>bootstrap</em>, not a configuration mechanism. It exists to solve
/// one problem — a fresh database has no account, so nobody can reach the roles
/// screen to create one — and it does nothing else. Every subsequent role and every
/// permission grant is made at runtime through the administration screens and
/// stored in the database.
/// </para>
/// <para>
/// The first role is seeded from <see cref="IPermissionCatalogue"/> rather than a
/// list written here, so no source file ever states which permissions a role has.
/// A new module's permissions join the catalogue by existing, and the mapping stays
/// editable without a deployment.
/// </para>
/// </summary>
internal static class DevelopmentBootstrap
{
    /// <summary>
    /// The single seeded role. Named as what it is — the account that bootstraps the
    /// others — so it reads as a starting point rather than a permanent fixture.
    /// </summary>
    private const string BootstrapRole = "Super Administrator";

    public static async Task BootstrapDevelopmentAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = Required<ILoggerFactory>(services).CreateLogger(nameof(DevelopmentBootstrap));

        await MigrateAsync(services);
        await SeedFirstAdministratorAsync(services, app.Configuration, logger);
    }

    private static async Task MigrateAsync(IServiceProvider services)
    {
        // One context for the whole application — master data and identity in a
        // single model with a single migration history — so this is one call rather
        // than a scan for every module's own context.
        if (services.GetService(typeof(ErpDbContext)) is DbContext context)
        {
            await context.Database.MigrateAsync();
        }
    }

    private static async Task SeedFirstAdministratorAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var userManager = Required<UserManager<ErpUser>>(services);
        var roleManager = Required<RoleManager<ErpRole>>(services);
        var catalogue = Required<IPermissionCatalogue>(services);

        var email = configuration["Bootstrap:AdminEmail"] ?? "admin@erp.local";

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            // Already bootstrapped. Notably this does not re-sync the role's
            // permissions: an administrator may have changed them, and the seed has
            // no business overriding a deliberate decision.
            return;
        }

        var role = await roleManager.FindByNameAsync(BootstrapRole);

        if (role is null)
        {
            role = new ErpRole(BootstrapRole)
            {
                Description = "Full access to everything, including permissions added by future modules.",

                // The flag, not a list of permission rows. A list would be a snapshot
                // of the catalogue on the day the database was created, and the account
                // would silently lose ground every time a module shipped. The claims
                // factory expands this against the live catalogue at each sign-in.
                IsSuperAdministrator = true,
            };

            await roleManager.CreateAsync(role);
        }

        var password = configuration["Bootstrap:AdminPassword"] ?? GeneratePassword();

        var admin = new ErpUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator",
            BusinessUnitId = 1,
            CanAccessAllBusinessUnits = true,
        };

        var created = await userManager.CreateAsync(admin, password);

        if (!created.Succeeded)
        {
            logger.LogError(
                "Could not seed the first administrator: {Errors}",
                string.Join(", ", created.Errors.Select(error => error.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, BootstrapRole);

        logger.LogWarning(
            "Bootstrapped super administrator {Email} with password {Password}. Full access to all "
            + "{Count} catalogued permissions, and to any added later. Development only — create "
            + "real roles through the roles screen.",
            email,
            password,
            catalogue.All.Count);
    }

    /// <summary>Satisfies the configured Identity policy: 12+ chars, mixed case, digit, symbol.</summary>
    private static string GeneratePassword() =>
        "Dev!" + RandomNumberGenerator.GetHexString(16, lowercase: false) + "a1";

    private static T Required<T>(IServiceProvider provider)
        where T : notnull =>
        // GetService(Type), not the GetRequiredService extension, which is a banned
        // symbol: hand-resolving services hides dependencies. This is the composition
        // root, where resolving them is the job.
        (T)(provider.GetService(typeof(T))
            ?? throw new InvalidOperationException($"{typeof(T).Name} is not registered."));
}
