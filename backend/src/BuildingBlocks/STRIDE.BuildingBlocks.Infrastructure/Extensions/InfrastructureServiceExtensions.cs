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

        return services;
    }

    /// <summary>
    /// Registers the MediatR-backed in-process event bus (ADR-014).
    /// Call this ONLY from STRIDE.Host — after all modules have been added so
    /// that <see cref="MediatR.IPublisher"/> is already in the container.
    /// The BFF has no MediatR handlers and must NOT call this method.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksEventBus(
        this IServiceCollection services)
    {
        services.AddScoped<IEventBus, MediatREventBus>();
        return services;
    }
}
