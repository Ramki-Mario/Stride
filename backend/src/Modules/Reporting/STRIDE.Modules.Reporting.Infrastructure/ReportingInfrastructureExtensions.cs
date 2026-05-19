using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Infrastructure.Persistence;
using STRIDE.Modules.Reporting.Infrastructure.Persistence.Repositories;
using STRIDE.Modules.Reporting.Infrastructure.ReadModels;

namespace STRIDE.Modules.Reporting.Infrastructure;

public static class ReportingInfrastructureExtensions
{
    public static IServiceCollection AddReportingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Teach Dapper to map SQL DATE columns to DateOnly (used in WorkflowTrendDto).
        // SqlMapper.AddTypeHandler is idempotent — safe to call on every startup.
        SqlMapper.AddTypeHandler(DateOnlyTypeHandler.Instance);

        services.AddDbContext<ReportingDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ReportingDbContext).Assembly.FullName)));

        services.AddScoped<IReportingReadService, ReportingReadService>();
        services.AddScoped<IReportRepository, ReportRepository>();

        return services;
    }
}
