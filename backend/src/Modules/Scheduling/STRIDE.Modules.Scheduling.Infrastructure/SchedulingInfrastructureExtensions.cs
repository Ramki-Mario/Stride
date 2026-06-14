using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Infrastructure.Persistence;
using STRIDE.Modules.Scheduling.Infrastructure.Persistence.Repositories;

namespace STRIDE.Modules.Scheduling.Infrastructure;

public static class SchedulingInfrastructureExtensions
{
    public static IServiceCollection AddSchedulingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SchedulingDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(SchedulingDbContext).Assembly.FullName)));

        services.AddScoped<IScheduleDefinitionRepository, ScheduleDefinitionRepository>();

        return services;
    }
}
