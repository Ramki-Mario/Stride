using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Behaviours;

namespace STRIDE.Modules.Teams.Application;

public static class TeamsApplicationExtensions
{
    public static IServiceCollection AddTeamsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(TeamsApplicationExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(typeof(TeamsApplicationExtensions).Assembly);

        return services;
    }
}
