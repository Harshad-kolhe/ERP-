using System.Data.Common;
using System.Net.Http.Json;
using System.Security.Claims;
using Erp.Api.Authentication;
using Erp.BuildingBlocks.Web.Security;
using Erp.Contracts.Auth;
using Erp.Modules.Masters;
using Erp.Persistence;
using Erp.Persistence.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace Erp.IntegrationTests;

/// <summary>
/// Boots the real application against a real SQL Server in a container.
/// <para>
/// Not the in-memory provider: it does not enforce unique indexes, filtered
/// indexes, <c>rowversion</c> concurrency or check constraints, so a test suite
/// built on it proves the C# compiles rather than that the schema is correct.
/// Every guarantee this system relies on — the tenant filter translating to SQL,
/// the duplicate part number being rejected by the index, a stale rowversion
/// producing a concurrency exception — only exists against a real database.
/// </para>
/// </summary>
public sealed class ErpApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    /// <summary>
    /// Reference data, which the reset must leave alone.
    /// <para>
    /// These tables are filled by a migration and are part of what the schema
    /// means, not data a test created: a part cannot be saved with a unit of
    /// measure the master has never heard of, so wiping the units between tests
    /// makes every subsequent create fail with a validation error that has nothing
    /// to do with the test. Respawn deletes by table, and reference data is exactly
    /// the case its <c>TablesToIgnore</c> exists for.
    /// </para>
    /// </summary>
    private static readonly Table[] ReferenceTables =
    [
        new("masters", "LookupValue"),
        new("masters", "UnitOfMeasure"),
        new("masters", "HsnCode"),
        new("masters", "HsnGstRate"),
    ];

    private Respawner? _respawner;
    private DbConnection? _resetConnection;

    private string ConnectionString => _sqlServer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _sqlServer.StartAsync();

        using var scope = Services.CreateScope();

        await MigrateAllContextsAsync(scope.ServiceProvider);
        await SeedUsersAsync(scope.ServiceProvider);

        _resetConnection = new SqlConnection(ConnectionString);
        await _resetConnection.OpenAsync();

        // Only the module schemas are reset between tests; the seeded identity
        // data survives, so sign-in does not have to be re-established each time.
        _respawner = await Respawner.CreateAsync(
            _resetConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude = ["masters"],
                TablesToIgnore = ReferenceTables,
            });
    }

    public async Task ResetAsync()
    {
        if (_respawner is not null && _resetConnection is not null)
        {
            await _respawner.ResetAsync(_resetConnection);
        }
    }

    /// <summary>
    /// A client that can hold the session cookie.
    /// <para>
    /// The base address must be <c>https</c>. The session cookie is issued with
    /// <c>SecurePolicy = Always</c>, so a cookie container will refuse to store or
    /// resend it over plain HTTP — and <see cref="WebApplicationFactory{T}.CreateClient"/>
    /// defaults to <c>http://localhost</c>. Every authenticated request would come
    /// back 401. The fix belongs here rather than in the cookie policy: requiring
    /// Secure in production is correct.
    /// </para>
    /// </summary>
    public HttpClient CreateWebClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    /// <summary>Signs in as the given user and returns a client carrying their session cookie.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(TestUser user)
    {
        var client = CreateWebClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = user.UserName, Password = TestUsers.Password });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Could not sign in as {user.UserName}: {response.StatusCode}");
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Erp", ConnectionString);
        builder.UseSetting("Observability:SeqUrl", "http://localhost:5341");
    }

    public override async ValueTask DisposeAsync()
    {
        if (_resetConnection is not null)
        {
            await _resetConnection.DisposeAsync();
        }

        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Migrates every module's DbContext plus Identity.
    /// <para>
    /// Contexts are found and resolved by <see cref="Type"/> rather than named
    /// directly, because a module's DbContext is <c>internal</c> and this test
    /// project deliberately has no access to it. The tests exercise the module
    /// through HTTP, exactly as any other caller would.
    /// </para>
    /// </summary>
    private static async Task MigrateAllContextsAsync(IServiceProvider services)
    {
        var contextTypes = new[] { typeof(MastersModule).Assembly, typeof(ErpDbContext).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(DbContext).IsAssignableFrom(type) && !type.IsAbstract)
            .Distinct();

        foreach (var contextType in contextTypes)
        {
            if (services.GetService(contextType) is DbContext context)
            {
                await context.Database.MigrateAsync();
            }
        }
    }

    private static async Task SeedUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ErpUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ErpRole>>();

        foreach (var testUser in TestUsers.All)
        {
            var role = new ErpRole(testUser.RoleName);
            await roleManager.CreateAsync(role);

            // Permissions hang off the role as role claims; the principal factory
            // flattens them onto the user at sign-in.
            foreach (var permission in testUser.Permissions)
            {
                await roleManager.AddClaimAsync(role, new Claim(ErpClaimTypes.Permission, permission));
            }

            var user = new ErpUser
            {
                UserName = testUser.UserName,
                Email = testUser.UserName,
                EmailConfirmed = true,
                DisplayName = testUser.UserName,
                BusinessUnitId = testUser.BusinessUnitId,
            };

            var created = await userManager.CreateAsync(user, TestUsers.Password);

            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed {testUser.UserName}: "
                    + string.Join(", ", created.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, testUser.RoleName);
        }
    }
}
