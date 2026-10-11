using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Rbac.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the real API in memory with its own SQLite database file and the seeded demo data.
/// Each test class gets a fresh instance through <c>IClassFixture</c>.
/// </summary>
public sealed class RbacApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"rbac-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Database:SeedOnStartup", "true");
        builder.UseSetting("RateLimiting:AuthenticationPermitLimit", "1000");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();

        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // The temp folder is cleaned by the OS eventually.
        }
    }
}
