using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Webhooks.Application;
using STRIDE.Modules.Webhooks.Infrastructure;

namespace STRIDE.Modules.Webhooks.API.Extensions;

public static class WebhooksModuleExtensions
{
    public static IServiceCollection AddWebhooksModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWebhooksApplication();
        services.AddWebhooksInfrastructure(configuration);
        return services;
    }
}
