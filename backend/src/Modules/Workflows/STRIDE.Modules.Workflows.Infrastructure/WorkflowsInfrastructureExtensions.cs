using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;
using STRIDE.Modules.Workflows.Infrastructure.ReadModels;

namespace STRIDE.Modules.Workflows.Infrastructure;

public static class WorkflowsInfrastructureExtensions
{
    public static IServiceCollection AddWorkflowsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ────────────────────────────────────────────────────────────
        services.AddDbContext<WorkflowsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(WorkflowsDbContext).Assembly.FullName)
                          .MigrationsHistoryTable("__EFMigrationsHistory", "workflows")));

        // ── Repositories (EF Core writes) ──────────────────────────────────────
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowInstanceRepository,   WorkflowInstanceRepository>();

        // ── Read service (Dapper reads) ────────────────────────────────────────
        services.AddScoped<IWorkflowReadService, WorkflowReadService>();

        return services;
    }
}
