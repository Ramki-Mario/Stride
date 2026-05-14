using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Workflows.Infrastructure.Persistence;

namespace STRIDE.Modules.Workflows.Infrastructure;

public static class WorkflowsInfrastructureExtensions
{
    public static IServiceCollection AddWorkflowsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WorkflowsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(WorkflowsDbContext).Assembly.FullName)));

        return services;
    }
}
