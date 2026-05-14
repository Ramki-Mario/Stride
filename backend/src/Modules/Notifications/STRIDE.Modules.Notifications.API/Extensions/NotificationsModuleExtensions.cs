using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Notifications.Application;
using STRIDE.Modules.Notifications.Infrastructure;

namespace STRIDE.Modules.Notifications.API.Extensions;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddNotificationsApplication();
        services.AddNotificationsInfrastructure(configuration);
        return services;
    }
}
