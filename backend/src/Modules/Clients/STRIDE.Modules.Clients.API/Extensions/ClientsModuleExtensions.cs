using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Clients.Application;
using STRIDE.Modules.Clients.Infrastructure;

namespace STRIDE.Modules.Clients.API.Extensions;

public static class ClientsModuleExtensions
{
    public static IServiceCollection AddClientsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddClientsApplication();
        services.AddClientsInfrastructure(configuration);
        return services;
    }
}
