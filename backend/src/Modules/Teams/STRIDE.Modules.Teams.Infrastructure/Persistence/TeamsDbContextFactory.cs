using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Teams.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add).
/// Not used at runtime — the real DbContext is registered via DI in TeamsInfrastructureExtensions.
/// </summary>
public sealed class TeamsDbContextFactory : IDesignTimeDbContextFactory<TeamsDbContext>
{
    public TeamsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TeamsDbContext>()
            .UseSqlServer(
                GetMigrationConnectionString(),
                sql => sql.MigrationsAssembly(typeof(TeamsDbContext).Assembly.FullName))
            .Options;

        return new TeamsDbContext(options);
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
