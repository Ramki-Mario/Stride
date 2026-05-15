using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Events;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.BuildingBlocks.Infrastructure.Tenant;

namespace STRIDE.BuildingBlocks.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<TenantContextProvider>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextProvider>());
        services.AddScoped<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContextProvider>());

        services.AddScoped<IEventBus, MediatREventBus>();

        return services;
    }
}
