using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Events;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.BuildingBlocks.Infrastructure.Identity;
using STRIDE.BuildingBlocks.Infrastructure.Tenant;

namespace STRIDE.BuildingBlocks.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tenant context — set by TenantMiddleware (authenticated) or by
        // Application handlers that resolve the tenant themselves (anonymous flows).
        services.AddScoped<TenantContextProvider>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextProvider>());
        services.AddScoped<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContextProvider>());

        // Current user — populated from JWT claims by the Host's JWT Bearer middleware.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddScoped<IEventBus, MediatREventBus>();

        return services;
    }
}
