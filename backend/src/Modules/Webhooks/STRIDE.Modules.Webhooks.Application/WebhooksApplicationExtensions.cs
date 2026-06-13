using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Behaviours;
using System.Reflection;

namespace STRIDE.Modules.Webhooks.Application;

public static class WebhooksApplicationExtensions
{
    public static IServiceCollection AddWebhooksApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
