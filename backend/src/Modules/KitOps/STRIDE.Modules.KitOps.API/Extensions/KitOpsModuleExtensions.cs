using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.KitOps.Application;
using STRIDE.Modules.KitOps.Infrastructure;

namespace STRIDE.Modules.KitOps.API.Extensions;

public static class KitOpsModuleExtensions
{
    public static IServiceCollection AddKitOpsModule(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddKitOpsApplication();
        services.AddKitOpsInfrastructure(configuration);
        return services;
    }
}
