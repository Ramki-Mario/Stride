using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Behaviours;
using System.Reflection;

namespace STRIDE.Modules.Notifications.Application;

public static class NotificationsApplicationExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // Pipeline: Logging → Validation → Handler
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        // Scan this assembly for all IValidator<T> implementations.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
