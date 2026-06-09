using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Teams.Application;
using STRIDE.Modules.Teams.Infrastructure;

namespace STRIDE.Modules.Teams.API.Extensions;

public static class TeamsModuleExtensions
{
    public static IServiceCollection AddTeamsModule(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddTeamsApplication();
        services.AddTeamsInfrastructure(configuration);
        return services;
    }
}
