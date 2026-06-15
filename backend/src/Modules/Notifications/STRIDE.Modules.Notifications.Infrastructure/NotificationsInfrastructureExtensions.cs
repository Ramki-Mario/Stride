using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Notifications.Application;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Infrastructure.Persistence;
using STRIDE.Modules.Notifications.Infrastructure.Persistence.Repositories;
using STRIDE.Modules.Notifications.Infrastructure.Services;

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
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

        // ── Cross-module query services ────────────────────────────────────────
        services.AddScoped<IUserRoleQueryService, UserRoleQueryService>();

        // ── Web Push ───────────────────────────────────────────────────────────
        services.AddSingleton<IWebPushService, WebPushService>();

        return services;
    }
}
