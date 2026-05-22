using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Behaviours;
using System.Reflection;

namespace STRIDE.Modules.Administration.Application;

public static class AdministrationApplicationExtensions
{
    public static IServiceCollection AddAdministrationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // Pipeline: Logging → Validation → Handler
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
