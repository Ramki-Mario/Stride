using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Invoicing.Infrastructure.Persistence;

namespace STRIDE.Modules.Invoicing.Infrastructure;

public static class InvoicingInfrastructureExtensions
{
    public static IServiceCollection AddInvoicingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InvoicingDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(InvoicingDbContext).Assembly.FullName)));

        return services;
    }
}
