using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;
using STRIDE.Modules.Identity.Infrastructure.TenantResolution;

namespace STRIDE.Modules.Identity.Infrastructure;

public static class IdentityInfrastructureExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

        services.AddScoped<ITenantResolver, TenantResolver>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}
