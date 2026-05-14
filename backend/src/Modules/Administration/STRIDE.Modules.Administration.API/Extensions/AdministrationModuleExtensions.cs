using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Administration.Application;
using STRIDE.Modules.Administration.Infrastructure;

namespace STRIDE.Modules.Administration.API.Extensions;

public static class AdministrationModuleExtensions
{
    public static IServiceCollection AddAdministrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAdministrationApplication();
        services.AddAdministrationInfrastructure(configuration);
        return services;
    }
}
