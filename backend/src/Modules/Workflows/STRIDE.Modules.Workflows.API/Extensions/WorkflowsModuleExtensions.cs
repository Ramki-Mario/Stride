using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Infrastructure;

namespace STRIDE.Modules.Workflows.API.Extensions;

public static class WorkflowsModuleExtensions
{
    public static IServiceCollection AddWorkflowsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWorkflowsApplication();
        services.AddWorkflowsInfrastructure(configuration);
        return services;
    }
}
