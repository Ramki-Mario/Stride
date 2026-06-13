using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Notifications.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add).
/// Not used at runtime — the real DbContext is registered via DI in NotificationsInfrastructureExtensions.
/// </summary>
internal sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();

        optionsBuilder.UseSqlServer(
            GetMigrationConnectionString(),
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications"));

        // No-op publisher — only needed for design-time tooling; never invoked during migrations.
        return new NotificationsDbContext(optionsBuilder.Options, new NoOpPublisher());
    }

    /// <summary>Stub publisher used only at design time when running EF Core migration commands.</summary>
    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
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
