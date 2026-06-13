using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add).
/// Not used at runtime — the real DbContext is registered via DI in WebhooksInfrastructureExtensions.
/// </summary>
public sealed class WebhooksDbContextFactory : IDesignTimeDbContextFactory<WebhooksDbContext>
{
    public WebhooksDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WebhooksDbContext>()
            .UseSqlServer(
                GetMigrationConnectionString(),
                sql => sql.MigrationsAssembly(typeof(WebhooksDbContext).Assembly.FullName))
            .Options;

        // Migrations never read/write real secret values, so a pass-through protector is sufficient.
        return new WebhooksDbContext(options, new PassThroughProtector());
    }

    private sealed class PassThroughProtector : IWebhookSecretProtector
    {
        public string Protect(string plaintext)    => plaintext;
        public string Unprotect(string ciphertext) => ciphertext;
    }

    private static string GetMigrationConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
        {
            var path = Path.Combine(d.FullName, "src", "Host", "STRIDE.Host", "appsettings.Development.json");
            if (!File.Exists(path)) continue;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("DefaultConnection", out var val))
                return val.GetString()
                    ?? throw new InvalidOperationException("DefaultConnection in appsettings.Development.json is null.");
        }

        throw new InvalidOperationException(
            "EF migration connection string not found. " +
            "Set ConnectionStrings__DefaultConnection as an environment variable, or ensure " +
            "appsettings.Development.json exists at src/Host/STRIDE.Host/ with a DefaultConnection value.");
    }
}
