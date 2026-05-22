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
            "Server=LAPTOP-417EMKN1\\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications"));

        // No-op publisher — only needed for design-time tooling; never invoked during migrations.
        return new NotificationsDbContext(optionsBuilder.Options, new NoOpPublisher());
    }

    /// <summary>Stub publisher used only at design time when running EF Core migration commands.</summary>
    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken ct = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
