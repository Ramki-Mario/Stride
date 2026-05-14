using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Invoicing.Application;
using STRIDE.Modules.Invoicing.Infrastructure;

namespace STRIDE.Modules.Invoicing.API.Extensions;

public static class InvoicingModuleExtensions
{
    public static IServiceCollection AddInvoicingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInvoicingApplication();
        services.AddInvoicingInfrastructure(configuration);
        return services;
    }
}
