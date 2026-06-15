using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.KitOps.Domain.Repositories;
using STRIDE.Modules.KitOps.Infrastructure.Persistence;

namespace STRIDE.Modules.KitOps.Infrastructure;

public static class KitOpsInfrastructureExtensions
{
    public static IServiceCollection AddKitOpsInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ITenantContext is resolved automatically by DI alongside DbContextOptions<KitOpsDbContext>.
        services.AddDbContext<KitOpsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(KitOpsDbContext).Assembly.FullName)));

        services.AddScoped<IKitItemRepository,        KitItemRepository>();
        services.AddScoped<IKitCheckoutRepository,    KitCheckoutRepository>();
        services.AddScoped<IKitReservationRepository, KitReservationRepository>();

        return services;
    }
}
