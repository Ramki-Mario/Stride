using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Behaviours;

namespace STRIDE.Modules.KitOps.Application;

public static class KitOpsApplicationExtensions
{
    public static IServiceCollection AddKitOpsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(KitOpsApplicationExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(typeof(KitOpsApplicationExtensions).Assembly);

        return services;
    }
}
