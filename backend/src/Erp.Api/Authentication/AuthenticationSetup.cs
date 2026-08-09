using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Authentication;

internal static class AuthenticationSetup
{
    public static IServiceCollection AddErpAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDataContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Erp"),
                sql => sql
                    .MigrationsHistoryTable("__EFMigrationsHistory", "identity")
                    .EnableRetryOnFailure()));

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services
            .AddIdentityCore<ErpUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // The legacy system had no lockout at all, and compared passwords
                // as plain strings, so it was open to unlimited offline-speed guessing.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ErpRole>()
            .AddEntityFrameworkStores<IdentityDataContext>()
            .AddClaimsPrincipalFactory<ErpClaimsPrincipalFactory>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "erp.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // This is an API. Unauthenticated requests get a status code, not a
            // 302 to a login page that the caller cannot render.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization(options =>
            // Fail closed. An endpoint that declares nothing still requires a
            // signed-in user; the legacy system left three whole controllers
            // reachable anonymously because someone omitted an attribute.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}
