using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Clients.Application.Abstractions;
using STRIDE.Modules.Clients.Domain.Repositories;
using STRIDE.Modules.Clients.Infrastructure.Persistence;
using STRIDE.Modules.Clients.Infrastructure.ReadModels;

namespace STRIDE.Modules.Clients.Infrastructure;

public static class ClientsInfrastructureExtensions
{
    public static IServiceCollection AddClientsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ClientsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ClientsDbContext).Assembly.FullName)));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientReadService, ClientReadService>();

        return services;
    }
}
