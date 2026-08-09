using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Erp.ArchitectureTests;

/// <summary>
/// Boots the real application so the tests can inspect what it actually built,
/// rather than what the source appears to say.
/// <para>
/// This is why these tests replaced the custom Roslyn analyzers originally planned:
/// a syntax analyzer can be defeated by an endpoint mapped through a helper, a loop
/// or a conditional. The endpoint table cannot — it is the same object the router
/// dispatches on at runtime.
/// </para>
/// <para>
/// No database is contacted. EF builds its model from the provider and the
/// configuration, and nothing here opens a connection.
/// </para>
/// </summary>
public sealed class ErpTestHost : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Erp",
            "Server=architecture-tests-does-not-connect;Database=Erp;Trusted_Connection=True;TrustServerCertificate=True");
        builder.UseSetting("Observability:SeqUrl", "http://localhost:5341");
    }
}
