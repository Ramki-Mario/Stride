using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Scheduling.Application;
using STRIDE.Modules.Scheduling.Infrastructure;

namespace STRIDE.Modules.Scheduling.API.Extensions;

public static class SchedulingModuleExtensions
{
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSchedulingApplication();
        services.AddSchedulingInfrastructure(configuration);
        return services;
    }
}
