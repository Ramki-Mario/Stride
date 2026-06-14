using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Services;
using STRIDE.Modules.Identity.Infrastructure.Auth;
using STRIDE.Modules.Identity.Infrastructure.Authorization;
using STRIDE.Modules.Identity.Infrastructure.Email;
using STRIDE.Modules.Identity.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;
using STRIDE.Modules.Identity.Infrastructure.TenantResolution;

namespace STRIDE.Modules.Identity.Infrastructure;

public static class IdentityInfrastructureExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserPermissionService, UserPermissionService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IInviteTokenRepository, InviteTokenRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ILoginResultBuilder, LoginResultBuilder>();

        // Email sender: SendGrid in production, console stub in development
        if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.Configure<SendGridOptions>(configuration.GetSection(SendGridOptions.SectionName));
            services.AddScoped<IEmailSender, SendGridEmailSender>();
        }

        return services;
    }
}
