using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Infrastructure.Persistence;
using STRIDE.Modules.Notifications.Infrastructure.Persistence.Repositories;

namespace STRIDE.Modules.Notifications.Infrastructure;

public static class NotificationsInfrastructureExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ────────────────────────────────────────────────────────────
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)
                          .MigrationsHistoryTable("__EFMigrationsHistory", "notifications")));

        // ── Repositories ───────────────────────────────────────────────────────
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
