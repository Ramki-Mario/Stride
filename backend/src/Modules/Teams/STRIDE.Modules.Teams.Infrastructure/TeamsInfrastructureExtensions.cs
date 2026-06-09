using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Teams.Application.Abstractions;
using STRIDE.Modules.Teams.Domain.Repositories;
using STRIDE.Modules.Teams.Infrastructure.Persistence;
using STRIDE.Modules.Teams.Infrastructure.ReadModels;

namespace STRIDE.Modules.Teams.Infrastructure;

public static class TeamsInfrastructureExtensions
{
    public static IServiceCollection AddTeamsInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddDbContext<TeamsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(TeamsDbContext).Assembly.FullName)));

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamReadService, TeamReadService>();

        return services;
    }
}
