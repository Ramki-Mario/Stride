using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Infrastructure.BackgroundJobs;
using STRIDE.Modules.Workflows.Infrastructure.FileStorage;
using STRIDE.Modules.Workflows.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;
using STRIDE.Modules.Workflows.Infrastructure.ReadModels;
using STRIDE.Modules.Workflows.Infrastructure.Services;

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
        services.AddScoped<IAttachmentRepository,         AttachmentRepository>();
        services.AddScoped<IWorkflowCommentRepository,    WorkflowCommentRepository>();
        services.AddScoped<IWorkflowActivityRepository,  WorkflowActivityRepository>();

        // ── Read service (Dapper reads) ────────────────────────────────────────
        services.AddScoped<IWorkflowReadService, WorkflowReadService>();

        // ── Cross-module role service (Dapper, identity schema) ────────────────
        services.AddScoped<IUserRoleService, UserRoleService>();

        // ── Background jobs ────────────────────────────────────────────────────
        // Hosted service that periodically scans for overdue steps and SLA breaches.
        services.AddHostedService<DeadlineCheckerService>();

        // ── File storage ───────────────────────────────────────────────────────
        // Switch between local (dev) and Azure Blob Storage (prod) via config.
        // Azure connection string must be provided via environment variable or
        // Azure Key Vault — never in committed appsettings files.
        var storageProvider = configuration["Storage:Provider"] ?? "local";
        if (storageProvider.Equals("azure", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        else
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
