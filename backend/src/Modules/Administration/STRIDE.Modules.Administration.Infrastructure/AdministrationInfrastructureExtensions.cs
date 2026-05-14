using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Administration.Infrastructure.Persistence;

namespace STRIDE.Modules.Administration.Infrastructure;

public static class AdministrationInfrastructureExtensions
{
    public static IServiceCollection AddAdministrationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AdministrationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AdministrationDbContext).Assembly.FullName)));

        return services;
    }
}
