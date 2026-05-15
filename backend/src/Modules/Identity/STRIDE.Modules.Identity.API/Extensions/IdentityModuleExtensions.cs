using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Identity.API.Authorization;
using STRIDE.Modules.Identity.Application;
using STRIDE.Modules.Identity.Infrastructure;

namespace STRIDE.Modules.Identity.API.Extensions;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityApplication();
        services.AddIdentityInfrastructure(configuration);

        // Register named RBAC policies declared by this module.
        // AddAuthorization is idempotent and additive — safe to call from multiple modules.
        services.AddIdentityAuthorizationPolicies();

        return services;
    }
}
