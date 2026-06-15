using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Infrastructure.Persistence;
using STRIDE.Modules.Administration.Infrastructure.ReadModels;
using STRIDE.Modules.Administration.Infrastructure.Services;

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

        // Dapper read and write services for the Administration module.
        services.AddScoped<IAdminReadService,     AdminReadService>();
        services.AddScoped<IAdminWriteService,    AdminWriteService>();
        services.AddScoped<IAuditLogReadService,  AuditLogReadService>();

        // Audit logger — fire-and-forget safe, Dapper-backed, cross-cutting.
        services.AddScoped<IAuditLogger, DapperAuditLogger>();

        // TenantSettings EF repository.
        services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();

        // Cross-schema resolver: reads Tenant.Name from identity.Tenants.
        services.AddScoped<ITenantNameResolver, TenantNameResolver>();

        // CSS sanitiser — stateless, safe as singleton.
        services.AddSingleton<ICssSanitiser, CssSanitiser>();

        // Module entitlement service — cross-cutting, used by the API gate filter.
        services.AddScoped<IModuleEntitlementService, ModuleEntitlementService>();

        return services;
    }
}
